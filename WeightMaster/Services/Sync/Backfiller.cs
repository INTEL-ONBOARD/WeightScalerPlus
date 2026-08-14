using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Uploads history that predates the triggers.
    ///
    /// The outbox only knows about changes made after the triggers were
    /// installed, so an existing branch would otherwise mirror only its future.
    /// This walks each table in ascending key order, a bounded chunk per cycle,
    /// recording its position so a closed app or a dropped line resumes rather
    /// than restarts.
    ///
    /// Deliberately rate-limited: a rural branch's connection is shared with the
    /// work of weighing, and this is the least urgent thing using it.
    /// </summary>
    public static class Backfiller
    {
        private const string Source = "SyncBackfill";

        public static async Task<int> RunChunkAsync(
            DbConnection local, DbConnection cloud, string branchId,
            int rowBudget, CancellationToken ct)
        {
            int moved = 0;

            foreach (string table in SyncTables.Operational)
            {
                if (moved >= rowBudget || ct.IsCancellationRequested) break;

                try
                {
                    moved += await RunTableAsync(
                        local, cloud, branchId, table, rowBudget - moved, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Error(Source, $"backfill failed for {table}", ex);
                }
            }

            return moved;
        }

        private static async Task<int> RunTableAsync(
            DbConnection local, DbConnection cloud, string branchId,
            string table, int budget, CancellationToken ct)
        {
            if (budget <= 0) return 0;

            (bool done, long position) = await ReadStateAsync(local, table, ct).ConfigureAwait(false);
            if (done) return 0;

            string? pkColumn = await RowMirror.GetPrimaryKeyAsync(local, table, ct).ConfigureAwait(false);
            if (pkColumn == null)
            {
                await MarkDoneAsync(local, table, ct).ConfigureAwait(false);
                return 0;
            }

            var rows = new List<Dictionary<string, object?>>();
            long highest = position;

            using (DbCommand cmd = local.CreateCommand())
            {
                cmd.CommandText =
                    $"SELECT * FROM `{table}` WHERE `{pkColumn}` > @pos " +
                    $"ORDER BY `{pkColumn}` LIMIT @take;";
                RowMirror.AddParam(cmd, "@pos", position);
                RowMirror.AddParam(cmd, "@take", budget);

                using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        object value = reader.GetValue(i);
                        row[reader.GetName(i)] = value is DBNull ? null : value;
                    }

                    if (row.TryGetValue(pkColumn, out object? pkValue) && pkValue != null)
                    {
                        long pk = Convert.ToInt64(pkValue);
                        if (pk > highest) highest = pk;
                    }

                    rows.Add(row);
                }
            }

            if (rows.Count == 0)
            {
                await MarkDoneAsync(local, table, ct).ConfigureAwait(false);
                Logger.Info(Source, $"backfill complete for {table}");
                return 0;
            }

            foreach (Dictionary<string, object?> row in rows)
            {
                ct.ThrowIfCancellationRequested();
                await RowMirror.UpsertAsync(cloud, table, branchId, row, ct).ConfigureAwait(false);
            }

            // Position advances only after the rows are safely in the cloud, so an
            // interruption re-sends a chunk rather than skipping it. Upserts are
            // idempotent, which makes re-sending harmless.
            await SavePositionAsync(local, table, highest, ct).ConfigureAwait(false);

            return rows.Count;
        }

        private static async Task<(bool done, long position)> ReadStateAsync(
            DbConnection local, string table, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText =
                "SELECT backfill_done, backfill_pos FROM sync_state WHERE table_name = @t LIMIT 1;";
            RowMirror.AddParam(cmd, "@t", table);

            using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return (false, 0);

            bool done = !reader.IsDBNull(0) && Convert.ToInt32(reader.GetValue(0)) == 1;
            long pos  = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1));

            return (done, pos);
        }

        private static async Task SavePositionAsync(
            DbConnection local, string table, long position, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = @"
INSERT INTO sync_state (table_name, backfill_pos, watermark_id)
VALUES (@t, @pos, @pos)
ON DUPLICATE KEY UPDATE
  backfill_pos = GREATEST(backfill_pos, VALUES(backfill_pos)),
  watermark_id = GREATEST(watermark_id, VALUES(watermark_id));";
            RowMirror.AddParam(cmd, "@t", table);
            RowMirror.AddParam(cmd, "@pos", position);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        private static async Task MarkDoneAsync(DbConnection local, string table, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = @"
INSERT INTO sync_state (table_name, backfill_done)
VALUES (@t, 1)
ON DUPLICATE KEY UPDATE backfill_done = 1;";
            RowMirror.AddParam(cmd, "@t", table);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
