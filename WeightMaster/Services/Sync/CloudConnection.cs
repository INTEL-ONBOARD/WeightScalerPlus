using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Minimal context used only to obtain a configured connection to the cloud
    /// database. The cloud tables carry extra columns and a different primary
    /// key, so they are written with explicit SQL rather than mapped entities.
    /// </summary>
    internal sealed class CloudDbContext : DbContext
    {
        private readonly string _connectionString;

        public CloudDbContext(string connectionString) => _connectionString = connectionString;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Parsed, not auto-detected: AutoDetect opens a connection during
                // construction, which would block startup on a slow link.
                optionsBuilder.UseMySql(_connectionString, ServerVersion.Parse("8.0.25"));
            }
        }
    }

    /// <summary>
    /// Opens and validates the connection to the cloud mirror.
    ///
    /// There is no separate "are we online?" probe anywhere in this subsystem.
    /// Attempting the connection and failing cheaply is the check.
    /// </summary>
    public static class CloudConnection
    {
        private const string Source = "SyncCloud";

        public static async Task<DbConnection?> OpenAsync(
            CloudSyncConfig config, CancellationToken ct = default)
        {
            if (!config.Enabled) return null;

            try
            {
                var ctx = new CloudDbContext(config.ConnectionString);
                DbConnection conn = ctx.Database.GetDbConnection();

                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct).ConfigureAwait(false);

                return conn;
            }
            catch (Exception ex)
            {
                // Expected whenever the branch is offline. Debug level, not error,
                // so a rural site with intermittent internet does not fill the log.
                Logger.Info(Source, $"cloud unreachable: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Confirms the cloud has the tables and mirror columns we expect.
        ///
        /// A mismatch disables syncing for this cycle rather than writing into a
        /// schema we do not understand. Stale beats corrupt.
        /// </summary>
        public static async Task<bool> VerifySchemaAsync(DbConnection conn, CancellationToken ct = default)
        {
            try
            {
                foreach (string table in SyncTables.Operational)
                {
                    if (!await HasMirrorColumnsAsync(conn, table, ct).ConfigureAwait(false))
                    {
                        Logger.Warn(Source,
                            $"cloud table '{table}' is missing or lacks mirror columns - sync paused");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "cloud schema verification failed", ex);
                return false;
            }
        }

        private static async Task<bool> HasMirrorColumnsAsync(
            DbConnection conn, string table, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COUNT(*) FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@table)
  AND COLUMN_NAME IN ('branch_id', 'synced_at', 'deleted_at');";

            DbParameter p = cmd.CreateParameter();
            p.ParameterName = "@table";
            p.Value = table;
            cmd.Parameters.Add(p);

            object? value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt32(value) == 3;
        }

        /// <summary>
        /// Records this branch's liveness so stragglers are visible centrally
        /// without anyone having to visit a site.
        /// </summary>
        public static async Task ReportStatusAsync(
            DbConnection conn, string branchId, string branchName, string appVersion,
            int pendingCount, string? lastError, bool success, CancellationToken ct = default)
        {
            try
            {
                using DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"
INSERT INTO sync_branch_status
  (branch_id, branch_name, app_version, last_contact_at, last_success_at, pending_count, last_error)
VALUES
  (@branch, @name, @version, NOW(3), IF(@ok, NOW(3), NULL), @pending, @error)
ON DUPLICATE KEY UPDATE
  branch_name     = VALUES(branch_name),
  app_version     = VALUES(app_version),
  last_contact_at = VALUES(last_contact_at),
  last_success_at = IF(@ok, VALUES(last_contact_at), last_success_at),
  pending_count   = VALUES(pending_count),
  last_error      = VALUES(last_error);";

                AddParam(cmd, "@branch", branchId);
                AddParam(cmd, "@name", branchName);
                AddParam(cmd, "@version", appVersion);
                AddParam(cmd, "@pending", pendingCount);
                AddParam(cmd, "@error", (object?)lastError ?? DBNull.Value);
                AddParam(cmd, "@ok", success);

                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Status reporting is diagnostics. It must never fail a sync cycle.
                Logger.Info(Source, $"could not report branch status: {ex.Message}");
            }
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
