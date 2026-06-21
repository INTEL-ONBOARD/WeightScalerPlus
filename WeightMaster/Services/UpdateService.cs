using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace WeightMaster.Services
{
    /// <summary>
    /// Result of an update check: the available version, the installer download URL and the release notes.
    /// </summary>
    public record UpdateInfo(Version Version, string DownloadUrl, string Notes);

    /// <summary>
    /// Checks GitHub Releases for a newer stable WeightMaster build, downloads the installer
    /// and launches it. The repo is public so no authentication token is required.
    /// </summary>
    public class UpdateService
    {
        // Public repo -> anonymous access. "/latest" excludes pre-releases (rc builds), giving us a stable-only channel.
        private const string LatestReleaseUrl =
            "https://api.github.com/repos/INTEL-ONBOARD/WeightScalerPlus/releases/latest";

        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
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

        /// <summary>
        /// Returns details of a newer stable release if one exists; otherwise null
        /// (already up to date, no stable release published yet, offline, or no installer asset).
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
            if (latest is null)
                return null;

            if (latest <= GetCurrentVersion())
                return null; // already on the latest (or newer) version

            // Find the installer asset (the *.exe produced by the Inno Setup step).
            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (!string.IsNullOrEmpty(name) && name!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(downloadUrl))
                return null; // release exists but has no installer attached

            var notes = root.TryGetProperty("body", out var body) ? (body.GetString() ?? string.Empty) : string.Empty;
            return new UpdateInfo(latest, downloadUrl!, notes);
        }

        /// <summary>Downloads the installer to %TEMP%, reporting 0-100 progress. Returns the local file path.</summary>
        public async Task<string> DownloadInstallerAsync(UpdateInfo info, IProgress<double>? progress = null)
        {
            var destination = Path.Combine(Path.GetTempPath(), $"WeightMaster-Setup-{info.Version}.exe");

            using var response = await _http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
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
            return destination;
        }

        /// <summary>
        /// Launches the downloaded installer (triggers the UAC prompt — the installer requires admin)
        /// and shuts the running app down so its files can be replaced.
        /// </summary>
        public void LaunchInstallerAndExit(string installerPath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true // required so Windows shows the elevation prompt
            });
            Application.Current.Shutdown();
        }

        // ----- "Automatic Updates" preference persistence -----
        // No shared settings store exists in the app, so persist this single flag next to the exe.
        private static string SettingsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_settings.json");

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
            // Tags look like "v26.4.0" or "26.4.0"; drop the leading v and any "-rc.N" suffix.
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
