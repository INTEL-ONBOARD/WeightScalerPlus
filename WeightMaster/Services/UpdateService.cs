using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WeightMaster.Services
{
    /// <summary>
    /// Result of an update check: the available version, the exe download URL and the release notes.
    /// </summary>
    public record UpdateInfo(Version Version, string DownloadUrl, string Notes);

    /// <summary>
    /// Checks GitHub Releases for a newer stable WeightMaster build and self-updates the portable,
    /// self-contained single-file exe in place. The repo is public so no authentication is required.
    /// Each release ships one exe per architecture (WeightMaster-win-x64.exe / WeightMaster-win-x86.exe).
    /// </summary>
    public class UpdateService
    {
        // Public repo -> anonymous access. "/latest" excludes pre-releases (rc builds): a stable-only channel.
        private const string LatestReleaseUrl =
            "https://api.github.com/repos/INTEL-ONBOARD/WeightScalerPlus/releases/latest";

        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) }; // the exe is ~75 MB
            // GitHub's REST API rejects requests without a User-Agent header with HTTP 403.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WeightMaster-Updater");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        /// <summary>Current running version, taken from the assembly version stamped by CI (Major.Minor.Build).</summary>
        public Version GetCurrentVersion()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            return Normalize(v);
        }

        /// <summary>The release asset matching this process's architecture, e.g. "WeightMaster-win-x64.exe".</summary>
        private static string ExpectedAssetName()
        {
            var rid = RuntimeInformation.ProcessArchitecture == Architecture.X86 ? "win-x86" : "win-x64";
            return $"WeightMaster-{rid}.exe";
        }

        /// <summary>
        /// Returns details of a newer stable release if one exists; otherwise null
        /// (already up to date, no stable release published yet, offline, or no exe asset).
        /// </summary>
        public async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            using var response = await _http.GetAsync(LatestReleaseUrl);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null; // no stable (non pre-release) release published yet
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
            var latest = ParseVersion(tag);
            if (latest is null || latest <= GetCurrentVersion())
                return null; // already on the latest (or newer) version

            // Prefer the exe that matches this process's architecture; fall back to any *.exe.
            var wanted = ExpectedAssetName();
            string? downloadUrl = null;
            string? fallbackUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrEmpty(name) || !name!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var url = asset.GetProperty("browser_download_url").GetString();
                    fallbackUrl ??= url;
                    if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = url;
                        break;
                    }
                }
            }
            downloadUrl ??= fallbackUrl;
            if (string.IsNullOrEmpty(downloadUrl))
                return null; // release exists but has no exe attached

            var notes = root.TryGetProperty("body", out var body) ? (body.GetString() ?? string.Empty) : string.Empty;
            return new UpdateInfo(latest, downloadUrl!, notes);
        }

        /// <summary>
        /// Self-update for the portable single-file exe: rename the running exe aside, download the new one
        /// into its place, relaunch it, then exit. (Windows allows renaming a running exe but not overwriting it.)
        /// </summary>
        public async Task DownloadAndApplyUpdateAsync(UpdateInfo info, IProgress<double>? progress = null)
        {
            var currentExe = Environment.ProcessPath
                ?? throw new InvalidOperationException("Could not resolve the running executable path.");
            var backup = currentExe + ".old";

            if (File.Exists(backup))
                File.Delete(backup);
            File.Move(currentExe, backup); // free up the path; the running process keeps executing from memory

            try
            {
                await DownloadToFileAsync(info.DownloadUrl, currentExe, progress);
            }
            catch
            {
                // Roll back so the app still works if the download failed.
                if (File.Exists(currentExe))
                    File.Delete(currentExe);
                File.Move(backup, currentExe);
                throw;
            }

            Process.Start(new ProcessStartInfo { FileName = currentExe, UseShellExecute = true });
            Application.Current.Shutdown();
        }

        /// <summary>Removes the previous exe left behind by a self-update. Call once on startup.</summary>
        public void CleanupOldVersion()
        {
            try
            {
                var backup = (Environment.ProcessPath ?? string.Empty) + ".old";
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            catch
            {
                // best-effort: a leftover .old file is harmless if it can't be removed yet
            }
        }

        private static async Task DownloadToFileAsync(string url, string destination, IProgress<double>? progress)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1L;

            await using var source = await response.Content.ReadAsStreamAsync();
            await using var target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory())) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read));
                received += read;
                if (total > 0)
                    progress?.Report(received * 100.0 / total);
            }
        }

        // ----- "Automatic Updates" preference persistence -----
        // No shared settings store exists in the app, so persist this single flag next to the exe.
        private static string SettingsPath =>
            Path.Combine(AppContext.BaseDirectory, "update_settings.json");

        public bool GetAutoUpdateEnabled()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return false;
                using var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
                return doc.RootElement.TryGetProperty("autoUpdate", out var p) && p.GetBoolean();
            }
            catch
            {
                return false;
            }
        }

        public void SetAutoUpdateEnabled(bool enabled)
        {
            try
            {
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new { autoUpdate = enabled }));
            }
            catch
            {
                // best-effort: a failure to persist the preference must not crash the app
            }
        }

        private static Version? ParseVersion(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;
            // Tags look like "v27.0.0" or "27.0.0"; drop the leading v and any "-rc.N" suffix.
            var s = tag!.TrimStart('v', 'V');
            var dash = s.IndexOf('-');
            if (dash >= 0)
                s = s.Substring(0, dash);
            return Version.TryParse(s, out var v) ? Normalize(v) : null;
        }

        private static Version Normalize(Version v) =>
            new Version(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0));
    }
}
