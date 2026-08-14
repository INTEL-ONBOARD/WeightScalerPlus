using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Creates the three sync-owned tables in the local database.
    ///
    /// Idempotent by construction (CREATE TABLE IF NOT EXISTS) so it is safe to
    /// run on every startup. Touches no existing table.
    /// </summary>
    public static class LocalSchemaInstaller
    {
        private const string Source = "SyncSchema";

        private const string CreateOutbox = @"
CREATE TABLE IF NOT EXISTS sync_outbox (
  seq        BIGINT AUTO_INCREMENT PRIMARY KEY,
  table_name VARCHAR(64)  NOT NULL,
  row_pk     BIGINT       NOT NULL,
  op         CHAR(1)      NOT NULL,
  changed_at DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB;";

        private const string CreateState = @"
CREATE TABLE IF NOT EXISTS sync_state (
  table_name    VARCHAR(64) NOT NULL PRIMARY KEY,
  watermark_id  BIGINT      NOT NULL DEFAULT 0,
  backfill_done TINYINT(1)  NOT NULL DEFAULT 0,
  backfill_pos  BIGINT      NOT NULL DEFAULT 0,
  last_sweep_at DATETIME    NULL,
  content_hash  CHAR(64)    NULL
) ENGINE=InnoDB;";

        private const string CreateApiLog = @"
CREATE TABLE IF NOT EXISTS api_post_log (
  seq           BIGINT AUTO_INCREMENT PRIMARY KEY,
  occurred_at   DATETIME(3)  NOT NULL,
  method        VARCHAR(8)   NOT NULL,
  endpoint      VARCHAR(255) NOT NULL,
  operation     VARCHAR(64)  NULL,
  source_table  VARCHAR(64)  NULL,
  source_row_pk BIGINT       NULL,
  request_json  LONGTEXT     NOT NULL,
  status        VARCHAR(16)  NOT NULL,
  http_status   INT          NULL,
  response_json LONGTEXT     NULL,
  error_message TEXT         NULL,
  duration_ms   INT          NULL,
  attempt       INT          NOT NULL DEFAULT 1,
  INDEX ix_api_post_log_occurred (occurred_at),
  INDEX ix_api_post_log_source (source_table, source_row_pk)
) ENGINE=InnoDB;";

        /// <summary>Why the last attempt failed, for the status file.</summary>
        public static string? LastError { get; private set; }

        /// <summary>
        /// Returns true when all three tables are present afterwards. Never
        /// throws, and never gives up permanently -- the caller retries, because
        /// the usual reason for failure is that the local MySQL has not finished
        /// starting yet.
        /// </summary>
        public static async Task<bool> EnsureAsync(CancellationToken ct = default)
        {
            string target = "unknown";

            try
            {
                using var db = new AppDbContext();

                // Recorded before connecting so a failure message names what it
                // could not reach. Carries no credentials.
                target = db.Database.GetDbConnection().DataSource ?? "unknown";

                await db.Database.ExecuteSqlRawAsync(CreateOutbox, ct).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(CreateState, ct).ConfigureAwait(false);
                await db.Database.ExecuteSqlRawAsync(CreateApiLog, ct).ConfigureAwait(false);

                Logger.Info(Source, $"sync tables present (local db: {target})");
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"{ex.GetType().Name}: {ex.Message} (local db: {target})";
                Logger.Warn(Source, $"could not create sync tables on {target} - will retry: {ex.Message}");
                return false;
            }
        }
    }
}
