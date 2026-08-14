using System;
using System.IO;
using System.Text.Json;

namespace WeightMaster.Config
{
    /// <summary>
    /// Settings for the cloud mirror, read from cloudsync.json beside the exe.
    ///
    /// These deliberately do NOT live in config.json. AppConfig deserialises that
    /// file into Dictionary&lt;string, string&gt; with no error handling, so a single
    /// non-string value there (a JSON bool, say) would throw and take the whole
    /// application down at startup. Keeping sync settings in their own file means
    /// existing installs need no config change at all.
    ///
    /// A missing, unreadable or malformed file yields Enabled = false. Absence is
    /// the safe default: no file, no sync, no behaviour change.
    /// </summary>
    public class CloudSyncConfig
    {
        public const string FileName = "cloudsync.json";

        public bool Enabled { get; private set; }
        public string ConnectionString { get; private set; } = string.Empty;
        public int IntervalSeconds { get; private set; } = 45;
        public int BatchSize { get; private set; } = 500;
        public int ApiLogRetentionDays { get; private set; } = 90;
        public int BackfillRowsPerCycle { get; private set; } = 2000;

        /// <summary>Why sync is off, when it is off. Surfaced in logs only.</summary>
        public string DisabledReason { get; private set; } = string.Empty;

        private CloudSyncConfig() { }

        public static CloudSyncConfig Disabled(string reason) =>
            new CloudSyncConfig { Enabled = false, DisabledReason = reason };

        /// <summary>
        /// Never throws. Any failure returns a disabled configuration, because a
        /// sync misconfiguration must never be able to stop the app from running.
        /// </summary>
        public static CloudSyncConfig Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

                if (!File.Exists(path))
                    return Disabled($"{FileName} not found");

                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return Disabled($"{FileName} is not a JSON object");

                var cfg = new CloudSyncConfig
                {
                    Enabled              = ReadBool(root, "sync_enabled", false),
                    ConnectionString     = ReadString(root, "connection_string", string.Empty),
                    IntervalSeconds      = ReadInt(root, "sync_interval_seconds", 45, 10, 3600),
                    BatchSize            = ReadInt(root, "sync_batch_size", 500, 50, 5000),
                    ApiLogRetentionDays  = ReadInt(root, "api_log_retention_days", 90, 1, 3650),
                    BackfillRowsPerCycle = ReadInt(root, "backfill_rows_per_cycle", 2000, 100, 50000)
                };

                if (cfg.Enabled && string.IsNullOrWhiteSpace(cfg.ConnectionString))
                    return Disabled("sync_enabled is true but connection_string is empty");

                if (!cfg.Enabled)
                    cfg.DisabledReason = "sync_enabled is false";

                return cfg;
            }
            catch (Exception ex)
            {
                return Disabled($"could not read {FileName}: {ex.Message}");
            }
        }

        // Tolerant readers: a wrong type or missing key falls back to the default
        // rather than failing the whole load.

        private static bool ReadBool(JsonElement root, string name, bool fallback)
        {
            if (!root.TryGetProperty(name, out JsonElement el)) return fallback;
            return el.ValueKind switch
            {
                JsonValueKind.True   => true,
                JsonValueKind.False  => false,
                JsonValueKind.String => bool.TryParse(el.GetString(), out bool b) ? b : fallback,
                _ => fallback
            };
        }

        private static string ReadString(JsonElement root, string name, string fallback)
        {
            if (!root.TryGetProperty(name, out JsonElement el)) return fallback;
            return el.ValueKind == JsonValueKind.String ? (el.GetString() ?? fallback) : fallback;
        }

        private static int ReadInt(JsonElement root, string name, int fallback, int min, int max)
        {
            int value = fallback;

            if (root.TryGetProperty(name, out JsonElement el))
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int n)) value = n;
                else if (el.ValueKind == JsonValueKind.String
                         && int.TryParse(el.GetString(), out int s)) value = s;
            }

            return Math.Clamp(value, min, max);
        }
    }
}
