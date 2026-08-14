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
    /// The background loop that pushes branch data to the cloud mirror.
    ///
    /// Every failure path ends in "try again next cycle". Nothing here can block
    /// a weighing, fail a save, or surface a dialog to an operator.
    /// </summary>
    public static class SyncWorker
    {
        private const string Source = "SyncWorker";
        private const string LockName = "wm_sync";

        private static CancellationTokenSource? _cts;
        private static Task? _loop;

        private static int _consecutiveFailures;

        public static bool IsRunning => _loop != null;
        public static DateTime? LastSuccessAt { get; private set; }
        public static string? LastError { get; private set; }

        public static void Start(CloudSyncConfig config)
        {
            if (_loop != null) return;

            if (!config.Enabled)
            {
                Logger.Info(Source, $"cloud sync disabled: {config.DisabledReason}");
                return;
            }

            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => LoopAsync(config, _cts.Token));

            Logger.Info(Source, $"cloud sync started (every {config.IntervalSeconds}s)");
        }

        public static async Task StopAsync()
        {
            if (_loop == null) return;

            _cts?.Cancel();

            try
            {
                await _loop.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn(Source, $"sync loop did not stop cleanly: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _loop = null;
            }
        }

        private static async Task LoopAsync(CloudSyncConfig config, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await RunCycleAsync(config, ct).ConfigureAwait(false);
                    _consecutiveFailures = 0;
                    LastSuccessAt = DateTime.Now;
                    LastError = null;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _consecutiveFailures++;
                    LastError = ex.Message;
                    Logger.Error(Source, "sync cycle failed", ex);
                }

                try
                {
                    await Task.Delay(NextDelay(config), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Logger.Info(Source, "cloud sync stopped");
        }

        /// <summary>
        /// Exponential backoff on repeated failure, capped at five minutes, so an
        /// offline branch retries occasionally rather than every cycle.
        /// </summary>
        private static TimeSpan NextDelay(CloudSyncConfig config)
        {
            if (_consecutiveFailures == 0)
                return TimeSpan.FromSeconds(config.IntervalSeconds);

            int factor = Math.Min(1 << Math.Min(_consecutiveFailures, 6), 64);
            int seconds = Math.Min(config.IntervalSeconds * factor, 300);

            return TimeSpan.FromSeconds(seconds);
        }

        private static async Task RunCycleAsync(CloudSyncConfig config, CancellationToken ct)
        {
            using var db = new AppDbContext();
            DbConnection local = db.Database.GetDbConnection();

            if (local.State != ConnectionState.Open)
                await local.OpenAsync(ct).ConfigureAwait(false);

            // Both stations run this application against the same branch database.
            // Without the lock they would drain the same outbox simultaneously.
            if (!await TryAcquireLockAsync(local, ct).ConfigureAwait(false))
                return;

            try
            {
                DbConnection? cloud = await CloudConnection.OpenAsync(config, ct).ConfigureAwait(false);
                if (cloud == null) return;   // offline; nothing lost

                try
                {
                    if (!await CloudConnection.VerifySchemaAsync(cloud, ct).ConfigureAwait(false))
                        return;

                    string branchId = SafeBranchId(out string branchName);

                    int applied = await OutboxDrainer
                        .DrainAsync(local, cloud, branchId, config.BatchSize, ct)
                        .ConfigureAwait(false);

                    int backfilled = await Backfiller
                        .RunChunkAsync(local, cloud, branchId, config.BackfillRowsPerCycle, ct)
                        .ConfigureAwait(false);

                    int pending = await OutboxDrainer.PendingCountAsync(local, ct).ConfigureAwait(false);

                    await CloudConnection.ReportStatusAsync(
                        cloud, branchId, branchName, AppVersion(), pending, null, true, ct)
                        .ConfigureAwait(false);

                    if (applied > 0 || backfilled > 0)
                        Logger.Info(Source, $"mirrored rows={applied} backfilled={backfilled} pending={pending}");
                }
                finally
                {
                    try { await cloud.CloseAsync().ConfigureAwait(false); } catch { /* ignore */ }
                }
            }
            finally
            {
                await ReleaseLockAsync(local).ConfigureAwait(false);
            }
        }

        private static async Task<bool> TryAcquireLockAsync(DbConnection local, CancellationToken ct)
        {
            using DbCommand cmd = local.CreateCommand();
            cmd.CommandText = $"SELECT GET_LOCK('{LockName}', 0);";

            object? value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return value != null && value != DBNull.Value && Convert.ToInt32(value) == 1;
        }

        private static async Task ReleaseLockAsync(DbConnection local)
        {
            try
            {
                using DbCommand cmd = local.CreateCommand();
                cmd.CommandText = $"SELECT RELEASE_LOCK('{LockName}');";
                await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            }
            catch
            {
                // The lock is released when the connection closes regardless.
            }
        }

        /// <summary>
        /// AppConfig throws when config.json is missing a key, so it is never
        /// allowed to take the sync loop down with it.
        /// </summary>
        private static string SafeBranchId(out string branchName)
        {
            try
            {
                var cfg = new AppConfig();
                branchName = cfg.branchName ?? string.Empty;
                return string.IsNullOrWhiteSpace(cfg.branchId) ? "UNKNOWN" : cfg.branchId;
            }
            catch
            {
                branchName = string.Empty;
                return "UNKNOWN";
            }
        }

        private static string AppVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
