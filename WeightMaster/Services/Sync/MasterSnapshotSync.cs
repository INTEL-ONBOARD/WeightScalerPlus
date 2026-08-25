using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Mirrors the three master tables by whole-table snapshot.
    ///
    /// UsersData, MembersData and lineMasterData are wiped and reinserted
    /// wholesale by the existing Replace*Async methods, so their identity keys
    /// change on every refresh and mean nothing across time. Row-level tracking
    /// would produce constant churn describing keys that are about to be thrown
    /// away.
    ///
    /// Instead: hash the table's contents, and when the hash changes replace this
    /// branch's cloud copy inside one transaction.
    /// </summary>
    public static class MasterSnapshotSync
    {
        private const string Source = "SyncMaster";

        public static async Task<int> RunAsync(
            DbConnection local, DbConnection cloud, string branchId, CancellationToken ct)
        {
            int replaced = 0;

            foreach (string table in SyncTables.Master)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    if (await RunTableAsync(local, cloud, branchId, table, ct).ConfigureAwait(false))
                        replaced++;
                }
                catch (Exception ex)
                {
                    Logger.Error(Source, $"snapshot sync failed for {table}", ex);
                }
            }

            return replaced;
        }

        private static async Task<bool> RunTableAsync(
            DbConnection local, DbConnection cloud, string branchId,
            string table, CancellationToken ct)
        {
            string? hash = await ComputeHashAsync(local, table, ct).ConfigureAwait(false);
            if (hash == null) return false;

            string? previous = await ReadHashAsync(local, table, ct).ConfigureAwait(false);
            if (previous == hash) return false;   // unchanged since last push

            List<Dictionary<string, object?>> rows =
                await ReadAllAsync(local, table, ct).ConfigureAwait(false);

            using DbTransaction tx = await cloud.BeginTransactionAsync(ct).ConfigureAwait(false);

            try
            {
                // Only this branch's copy is cleared. Other branches are untouched.
                using (DbCommand del = cloud.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = $"DELETE FROM `{table}` WHERE `branch_id` = @branch;";
                    RowMirror.AddParam(del, "@branch", branchId);
                    await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                foreach (Dictionary<string, object?> row in rows)
                {
                    ct.ThrowIfCancellationRequested();
                    await RowMirror.UpsertAsync(cloud, table, branchId, row, ct, tx).ConfigureAwait(false);
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                try { await tx.RollbackAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }
                throw;
            }

            // Recorded only after a successful commit, so a failure retries next cycle.
            await SaveHashAsync(local, table, hash, ct).ConfigureAwait(false);

            Logger.Info(Source, $"replaced cloud copy of {table} ({rows.Count} rows)");
            return true;
        }

        /// <summary>
        /// Cheap whole-table fingerprint: row count plus a checksum. Enough to
        /// notice a wholesale replace, which is the only way these tables change.
        /// </summary>
        private static async Task<string?> ComputeHashAsync(
            DbConnection local, string table, CancellationToken ct)
        {
            try
            {
                object checksum;

                // The reader MUST be closed before anything else runs on this
                // connection. MySqlConnector allows one active result set per
                // connection, so counting while the CHECKSUM reader was still open
                // threw on every cycle - and because the failure was swallowed and
                // logged at Info, the master tables silently never synced at all.
                using (DbCommand cmd = local.CreateCommand())
                {
                    cmd.CommandText = $"CHECKSUM TABLE `{table}`;";

                    using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                    if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;

                    object value = reader.GetValue(reader.FieldCount - 1);
                    if (value is DBNull) return null;

                    checksum = value;
                }

                long count = await CountAsync(local, table, ct).ConfigureAwait(false);
                return $"{checksum}:{count}";
            }
            catch (Exception ex)
            {
                // Warn, not Info: this stops a whole table from ever mirroring, and
                // at Info it was invisible among thousands of routine event lines.
                Logger.Warn(Source, $"could not checksum {table} - it will not mirror: {ex.Message}");
                return null;
            }
        }

        private static async Task<long> CountAsync(DbConnection local, string table, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM `{table}`;";
            return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
        }

        private static async Task<List<Dictionary<string, object?>>> ReadAllAsync(
            DbConnection local, string table, CancellationToken ct)
        {
            var rows = new List<Dictionary<string, object?>>();

            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = $"SELECT * FROM `{table}`;";

            using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    object value = reader.GetValue(i);
                    row[reader.GetName(i)] = value is DBNull ? null : value;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static async Task<string?> ReadHashAsync(
            DbConnection local, string table, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = "SELECT content_hash FROM sync_state WHERE table_name = @t LIMIT 1;";
            RowMirror.AddParam(cmd, "@t", table);

            return (await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)) as string;
        }

        private static async Task SaveHashAsync(
            DbConnection local, string table, string hash, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = @"
INSERT INTO sync_state (table_name, content_hash) VALUES (@t, @h)
ON DUPLICATE KEY UPDATE content_hash = VALUES(content_hash);";
            RowMirror.AddParam(cmd, "@t", table);
            RowMirror.AddParam(cmd, "@h", hash);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }
}
