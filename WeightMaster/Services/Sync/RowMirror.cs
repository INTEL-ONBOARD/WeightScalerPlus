using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Moves individual rows from the branch database to the cloud.
    ///
    /// Everything here is idempotent. A row is always sent as its current state
    /// rather than as a change, so replaying a batch after a dropped connection
    /// produces exactly the same result as sending it once.
    /// </summary>
    public static class RowMirror
    {
        private static readonly ConcurrentDictionary<string, List<string>> _columnCache = new();
        private static readonly ConcurrentDictionary<string, string> _pkCache = new();

        public static void ClearCaches()
        {
            _columnCache.Clear();
            _pkCache.Clear();
        }

        public static async Task<List<string>> GetColumnsAsync(
            DbConnection conn, string table, CancellationToken ct)
        {
            if (_columnCache.TryGetValue(table, out List<string>? cached)) return cached;

            var columns = new List<string>();

            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COLUMN_NAME FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table
ORDER BY ORDINAL_POSITION;";
                AddParam(cmd, "@table", table);

                using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    columns.Add(reader.GetString(0));
            }

            _columnCache[table] = columns;
            return columns;
        }

        public static async Task<string?> GetPrimaryKeyAsync(
            DbConnection conn, string table, CancellationToken ct)
        {
            if (_pkCache.TryGetValue(table, out string? cached)) return cached;

            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND CONSTRAINT_NAME = 'PRIMARY'
LIMIT 1;";
            AddParam(cmd, "@table", table);

            if ((await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)) is string pk)
            {
                _pkCache[table] = pk;
                return pk;
            }

            return null;
        }

        /// <summary>
        /// Reads a row's current state. Returns null when the row no longer
        /// exists, which the caller treats as a deletion.
        /// </summary>
        public static async Task<Dictionary<string, object?>?> ReadRowAsync(
            DbConnection conn, string table, string pkColumn, long pk, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM `{table}` WHERE `{pkColumn}` = @pk LIMIT 1;";
            AddParam(cmd, "@pk", pk);

            using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;

            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                object value = reader.GetValue(i);
                row[reader.GetName(i)] = value is DBNull ? null : value;
            }

            return row;
        }

        /// <summary>
        /// Writes one row to the cloud, creating or overwriting as needed.
        /// Clears deleted_at, so a row that reappears locally is un-deleted.
        /// </summary>
        public static async Task UpsertAsync(
            DbConnection cloud, string table, string branchId,
            Dictionary<string, object?> row, CancellationToken ct,
            DbTransaction? tx = null)
        {
            var names = new List<string>(row.Count);
            foreach (string key in row.Keys) names.Add(key);

            var insertCols = new StringBuilder("`branch_id`");
            var insertVals = new StringBuilder("@branch_id");
            var updates    = new StringBuilder();

            for (int i = 0; i < names.Count; i++)
            {
                insertCols.Append(", `").Append(names[i]).Append('`');
                insertVals.Append(", @p").Append(i);

                if (updates.Length > 0) updates.Append(", ");
                updates.Append('`').Append(names[i]).Append("` = VALUES(`").Append(names[i]).Append("`)");
            }

            insertCols.Append(", `synced_at`, `deleted_at`");
            insertVals.Append(", NOW(3), NULL");
            updates.Append(", `synced_at` = NOW(3), `deleted_at` = NULL");

            using DbCommand cmd = cloud.CreateCommand();

            // Required when the caller has a transaction open on this connection:
            // MySqlConnector refuses to run a command that is not enlisted in the
            // connection's active transaction. Null for the ordinary autocommit path.
            cmd.Transaction = tx;

            cmd.CommandText =
                $"INSERT INTO `{table}` ({insertCols}) VALUES ({insertVals}) " +
                $"ON DUPLICATE KEY UPDATE {updates};";

            AddParam(cmd, "@branch_id", branchId);

            for (int i = 0; i < names.Count; i++)
                AddParam(cmd, $"@p{i}", row[names[i]] ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Soft-deletes a row. The mirror never destroys history, and a wrongly
        /// detected deletion stays recoverable.
        /// </summary>
        public static async Task MarkDeletedAsync(
            DbConnection cloud, string table, string branchId,
            string pkColumn, long pk, CancellationToken ct)
        {
            using DbCommand cmd = cloud.CreateCommand();
            cmd.CommandText =
                $"UPDATE `{table}` SET `deleted_at` = NOW(3), `synced_at` = NOW(3) " +
                $"WHERE `branch_id` = @branch AND `{pkColumn}` = @pk AND `deleted_at` IS NULL;";

            AddParam(cmd, "@branch", branchId);
            AddParam(cmd, "@pk", pk);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        internal static void AddParam(DbCommand cmd, string name, object value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
