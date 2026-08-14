using System;
using System.Threading.Tasks;
using WeightMaster.Config;
using WeightMaster.Services.Api;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Single entry point for turning the cloud mirror on at startup.
    ///
    /// Everything is behind cloudsync.json. No file means disabled, which is why
    /// existing installations need no configuration change and behave exactly as
    /// they do today.
    ///
    /// Nothing in here is awaited by the UI thread and nothing here can throw
    /// into the application. The worst outcome is that sync stays off.
    /// </summary>
    public static class SyncBootstrap
    {
        private const string Source = "SyncBootstrap";

        public static CloudSyncConfig? Config { get; private set; }

        /// <summary>
        /// Fire-and-forget from application startup. Safe to call more than once.
        /// </summary>
        public static void StartInBackground()
        {
            Task.Run(async () =>
            {
                try
                {
                    await StartAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Error(Source, "cloud mirror failed to start - continuing without it", ex);
                }
            });
        }

        private static async Task StartAsync()
        {
            CloudSyncConfig config = CloudSyncConfig.Load();
            Config = config;

            if (!config.Enabled)
            {
                Logger.Info(Source, $"cloud mirror off: {config.DisabledReason}");
                return;
            }

            // Local tables first: without them there is nothing to record into.
            if (!await LocalSchemaInstaller.EnsureAsync().ConfigureAwait(false))
            {
                Logger.Warn(Source, "sync tables unavailable - cloud mirror stays off");
                return;
            }

            // Audit capture is useful on its own and does not need the cloud.
            ApiAuditWriter.Start();

            TriggerInstallResult triggers = await TriggerInstaller.EnsureAsync().ConfigureAwait(false);

            if (triggers.Created + triggers.AlreadyCurrent + triggers.Replaced == 0)
                Logger.Warn(Source, "no change-capture triggers installed - relying on backfill only");

            // A convenience for the operator: the DDL to apply to the cloud once.
            await CloudSchemaScript.GenerateAsync().ConfigureAwait(false);

            SyncWorker.Start(config);

            Logger.Event("cloud_mirror_started", new
            {
                interval = config.IntervalSeconds,
                batch = config.BatchSize,
                triggers = triggers.ToString()
            });
        }

        /// <summary>Called on shutdown so in-flight audit rows are flushed.</summary>
        public static async Task StopAsync()
        {
            try
            {
                await SyncWorker.StopAsync().ConfigureAwait(false);
                await ApiAuditWriter.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn(Source, $"cloud mirror did not stop cleanly: {ex.Message}");
            }
        }
    }
}
