using System;
using System.IO;
using Newtonsoft.Json;

namespace WeightMaster.Services
{
    /// <summary>
    /// Append-only "JSON Lines" logger: writes one JSON object per line to log.json next to the
    /// running EXE (same folder as dbconfig.txt / config.json).
    ///
    /// Why this exists: the app ships as a Release build, where every System.Diagnostics.Debug.WriteLine
    /// is compiled out — so in production the app logged nothing, and errors vanished silently. This
    /// logger does its own file writes, so it works in the shipped EXE.
    ///
    /// Guarantees: thread-safe (lock around each append), crash-safe (each call flushes/closes the
    /// file), and it NEVER throws — a logging failure can never crash the app.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _path =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.json");

        private static string _version = "";
        private static string _branch = "";

        /// <summary>Stamp every log line with the app version + station branch. Call once at startup.</summary>
        public static void Init(string? version, string? branch)
        {
            _version = version ?? "";
            _branch = branch ?? "";
        }

        public static void Info(string source, string message, object? ctx = null)
            => Write("INFO", source, message, null, ctx);

        public static void Warn(string source, string message, object? ctx = null)
            => Write("WARN", source, message, null, ctx);

        public static void Error(string source, string message, Exception? ex = null, object? ctx = null)
            => Write("ERROR", source, message, ex, ctx);

        /// <summary>Log a business event at INFO level, e.g. Event("station2_confirm", new { member }).</summary>
        public static void Event(string name, object? data = null)
            => Write("INFO", "Event", name, null, data);

        private static void Write(string level, string source, string message, Exception? ex, object? ctx)
        {
            try
            {
                var entry = new
                {
                    ts = DateTimeOffset.Now.ToString("o"),
                    level,
                    source,
                    msg = message,
                    ver = _version,
                    branch = _branch,
                    ctx,
                    ex = ex == null ? null : new
                    {
                        type = ex.GetType().FullName,
                        msg = ex.Message,
                        stack = ex.StackTrace,
                        inner = ex.InnerException?.Message
                    }
                };

                string line = JsonConvert.SerializeObject(entry,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                lock (_lock)
                {
                    File.AppendAllText(_path, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never crash the app — swallow any file/serialization error.
            }
        }
    }
}
