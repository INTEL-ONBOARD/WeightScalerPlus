using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace WeightMaster.Services.Sync
{
    internal sealed class OutboxItem
    {
        public long Seq;
        public string TableName = string.Empty;
        public long RowPk;
        public string Op = string.Empty;
    }

    /// <summary>
    /// Drains pending change notifications and applies them to the cloud.
    ///
    /// Notifications are collapsed before any work is done: ten edits to one row
    /// become a single upsert of that row's current state. The outbox is a list
    /// of what to look at, never a list of what to replay.
    /// </summary>
    public static class OutboxDrainer
    {
        private const string Source = "SyncDrain";

        public static async Task<int> DrainAsync(
            DbConnection local, DbConnection cloud, string branchId,
            int batchSize, CancellationToken ct)
        {
            List<OutboxItem> batch = await ReadBatchAsync(local, batchSize, ct).ConfigureAwait(false);
            if (batch.Count == 0) return 0;

            // Collapse to the latest intent per (table, row).
            var latest = new Dictionary<string, OutboxItem>();
            long highestSeq = 0;

            foreach (OutboxItem item in batch)
            {
                latest[$"{item.TableName}{item.RowPk}"] = item;
                if (item.Seq > highestSeq) highestSeq = item.Seq;
            }

            int applied = 0;

            foreach (OutboxItem item in latest.Values)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (await ApplyAsync(local, cloud, branchId, item, ct).ConfigureAwait(false))
                        applied++;
                }
                catch (Exception ex)
                {
                    // Leave the entry in place; the next cycle retries it. Nothing
                    // is lost, and one poisoned row cannot stall the whole batch.
                    Logger.Error(Source,
                        $"could not mirror {item.TableName}#{item.RowPk}", ex);
                }
            }

            await DeleteUpToAsync(local, highestSeq, ct).ConfigureAwait(false);
            return applied;
        }

        private static async Task<bool> ApplyAsync(
            DbConnection local, DbConnection cloud, string branchId,
            OutboxItem item, CancellationToken ct)
        {
            string? pkColumn = await RowMirror.GetPrimaryKeyAsync(local, item.TableName, ct)
                                              .ConfigureAwait(false);
            if (pkColumn == null) return false;

            Dictionary<string, object?>? row =
                await RowMirror.ReadRowAsync(local, item.TableName, pkColumn, item.RowPk, ct)
                               .ConfigureAwait(false);

            if (row == null)
            {
                // Gone locally, whatever the recorded operation said. Trusting the
                // current state rather than the notification keeps a stale 'U'
                // from resurrecting a deleted row.
                await RowMirror.MarkDeletedAsync(cloud, item.TableName, branchId, pkColumn, item.RowPk, ct)
                               .ConfigureAwait(false);
                return true;
            }

            await RowMirror.UpsertAsync(cloud, item.TableName, branchId, row, ct).ConfigureAwait(false);
            return true;
        }

        private static async Task<List<OutboxItem>> ReadBatchAsync(
            DbConnection local, int batchSize, CancellationToken ct)
        {
            var items = new List<OutboxItem>();

            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText =
                "SELECT seq, table_name, row_pk, op FROM sync_outbox ORDER BY seq LIMIT @take;";
            RowMirror.AddParam(cmd, "@take", batchSize);

            using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                items.Add(new OutboxItem
                {
                    Seq       = reader.GetInt64(0),
                    TableName = reader.GetString(1),
                    RowPk     = reader.GetInt64(2),
                    Op        = reader.GetString(3)
                });
            }

            return items;
        }

        private static async Task DeleteUpToAsync(DbConnection local, long seq, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = "DELETE FROM sync_outbox WHERE seq <= @seq;";
            RowMirror.AddParam(cmd, "@seq", seq);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        public static async Task<int> PendingCountAsync(DbConnection local, CancellationToken ct)
        {
            try
            {
                using DbCommand cmd = local.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sync_outbox;";
                return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
            }
            catch
            {
                return -1;
            }
        }
    }
}
