using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Generates the cloud-side DDL by reading the local schema.
    ///
    /// Written this way rather than hand-authored so the cloud columns cannot
    /// drift from what the branch actually has. The application never executes
    /// DDL against the cloud; it writes a script for an operator to apply once.
    /// </summary>
    public static class CloudSchemaScript
    {
        private const string Source = "SyncSchemaGen";
        public const string FileName = "cloud-schema.sql";

        /// <summary>
        /// Writes cloud-schema.sql beside the exe. Returns the path, or null on
        /// failure. Never throws.
        /// </summary>
        public static async Task<string?> GenerateAsync(CancellationToken ct = default)
        {
            try
            {
                using var db = new AppDbContext();
                DbConnection conn = db.Database.GetDbConnection();

                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct).ConfigureAwait(false);

                var sql = new StringBuilder();

                sql.AppendLine("-- Cloud mirror schema for WeightMaster.");
                sql.AppendLine("-- Generated from a branch's local schema. Apply once to the cloud database.");
                sql.AppendLine("-- Every mirrored table gains branch_id / synced_at / deleted_at,");
                sql.AppendLine("-- and is keyed on (branch_id, <local primary key>) so branches cannot collide.");
                sql.AppendLine();
                sql.AppendLine(BranchStatusTable());

                var allTables = new List<string>();
                allTables.AddRange(SyncTables.Operational);
                allTables.AddRange(SyncTables.Master);

                foreach (string logicalName in allTables)
                {
                    string? table = await ResolveTableAsync(conn, logicalName, ct).ConfigureAwait(false);

                    if (table == null)
                    {
                        sql.AppendLine($"-- SKIPPED: table '{logicalName}' not present in this branch.");
                        sql.AppendLine();
                        continue;
                    }

                    string ddl = await BuildTableAsync(conn, table, ct).ConfigureAwait(false);
                    sql.AppendLine(ddl);
                    sql.AppendLine();
                }

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
                await File.WriteAllTextAsync(path, sql.ToString(), ct).ConfigureAwait(false);

                Logger.Info(Source, $"cloud schema script written to {path}");
                return path;
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "could not generate cloud schema script", ex);
                return null;
            }
        }

        private static string BranchStatusTable() => @"CREATE TABLE IF NOT EXISTS sync_branch_status (
  branch_id       VARCHAR(32) NOT NULL PRIMARY KEY,
  branch_name     VARCHAR(128) NULL,
  app_version     VARCHAR(32)  NULL,
  last_contact_at DATETIME(3)  NULL,
  last_success_at DATETIME(3)  NULL,
  pending_count   INT          NULL,
  last_error      TEXT         NULL
) ENGINE=InnoDB;";

        private static async Task<string?> ResolveTableAsync(
            DbConnection conn, string logicalName, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT TABLE_NAME FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@name) LIMIT 1;";
            AddParam(cmd, "@name", logicalName);

            return (await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false)) as string;
        }

        private static async Task<string> BuildTableAsync(
            DbConnection conn, string table, CancellationToken ct)
        {
            var columns = new List<string>();
            string primaryKey = "id";

            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_KEY
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table
ORDER BY ORDINAL_POSITION;";
                AddParam(cmd, "@table", table);

                using DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    string name     = reader.GetString(0);
                    string type     = reader.GetString(1);
                    bool   nullable = reader.GetString(2) == "YES";
                    string key      = reader.GetString(3);

                    if (key == "PRI") primaryKey = name;

                    // No AUTO_INCREMENT on the cloud: keys are assigned by the
                    // branch, never generated centrally.
                    columns.Add($"  `{name}` {type} {(nullable ? "NULL" : "NOT NULL")}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE IF NOT EXISTS `{table}` (");
            sb.AppendLine("  `branch_id` VARCHAR(32) NOT NULL,");

            foreach (string col in columns)
                sb.AppendLine(col + ",");

            sb.AppendLine("  `synced_at` DATETIME(3) NOT NULL,");
            sb.AppendLine("  `deleted_at` DATETIME(3) NULL,");
            sb.AppendLine($"  PRIMARY KEY (`branch_id`, `{primaryKey}`),");
            sb.AppendLine("  INDEX `ix_synced_at` (`synced_at`)");
            sb.Append(") ENGINE=InnoDB;");

            return sb.ToString();
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
