using System;
using System.IO;
using System.Threading;
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
    /// Nothing here is awaited by the UI thread and nothing here can throw into
    /// the application. The worst outcome is that sync stays off.
    /// </summary>
    public static class SyncBootstrap
    {
        private const string Source = "SyncBootstrap";
        public const string StatusFileName = "cloudsync-status.txt";

        /// <summary>
        /// The local MySQL is commonly still starting when this runs, so the first
        /// attempt waits rather than racing it.
        /// </summary>
        private static readonly TimeSpan FirstAttemptDelay = TimeSpan.FromSeconds(20);

        private static readonly TimeSpan[] RetryDelays =
        {
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5)
        };

        /// <summary>Roughly an hour of retrying before giving up for this session.</summary>
        private const int MaxAttempts = 20;

        public static CloudSyncConfig? Config { get; private set; }
        public static bool Started { get; private set; }

        public static void StartInBackground()
        {
            Task.Run(async () =>
            {
                try
                {
                    await RunAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Error(Source, "cloud mirror failed to start - continuing without it", ex);
                    WriteStatus("ERROR", $"unexpected failure: {ex.Message}");
                }
            });
        }

        private static async Task RunAsync()
        {
            CloudSyncConfig config = CloudSyncConfig.Load();
            Config = config;

            if (!config.Enabled)
            {
                Logger.Info(Source, $"cloud mirror off: {config.DisabledReason}");
                WriteStatus("OFF", config.DisabledReason);
                return;
            }

            WriteStatus("STARTING", "waiting for the local database");

            // The local database is the prerequisite for everything else. Failing
            // to reach it once is not a reason to stay off for the whole session:
            // at startup it usually just means MySQL is not up yet.
            await Task.Delay(FirstAttemptDelay).ConfigureAwait(false);

            bool ready = false;

            for (int attempt = 1; attempt <= MaxAttempts && !ready; attempt++)
            {
                ready = await LocalSchemaInstaller.EnsureAsync().ConfigureAwait(false);

                if (ready) break;

                TimeSpan wait = RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)];

                WriteStatus("RETRYING",
                    $"attempt {attempt}/{MaxAttempts} failed, retrying in {wait.TotalSeconds:0}s. " +
                    $"{LocalSchemaInstaller.LastError}");

                await Task.Delay(wait).ConfigureAwait(false);
            }

            if (!ready)
            {
                Logger.Error(Source,
                    $"local database unreachable after {MaxAttempts} attempts - cloud mirror stays off");
                WriteStatus("OFF", $"local database unreachable. {LocalSchemaInstaller.LastError}");
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
            Started = true;

            WriteStatus("ON",
                $"syncing every {config.IntervalSeconds}s. triggers: {triggers}");

            Logger.Event("cloud_mirror_started", new
            {
                interval = config.IntervalSeconds,
                batch = config.BatchSize,
                triggers = triggers.ToString()
            });
        }

        /// <summary>
        /// Writes a one-line plain-text status beside the exe.
        ///
        /// Without this the only evidence of a problem is a line buried in
        /// log.json, which means someone has to know to go looking. Anyone at a
        /// branch can open this file and read what the mirror is doing.
        /// </summary>
        private static void WriteStatus(string state, string detail)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StatusFileName);

                string text =
                    $"cloud mirror: {state}{Environment.NewLine}" +
                    $"detail      : {detail}{Environment.NewLine}" +
                    $"updated     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}";

                File.WriteAllText(path, text);
            }
            catch
            {
                // Diagnostics must never be the thing that breaks.
            }
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
