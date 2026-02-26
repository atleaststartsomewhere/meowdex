using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Meowdex.Desktop.Services;

public sealed class AppUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _manifestUrl;
    private readonly object _sync = new();
    private UpdateCheckResult? _lastResult;

    public AppUpdateService(HttpClient httpClient, string manifestUrl)
    {
        _httpClient = httpClient;
        _manifestUrl = manifestUrl;
    }

    public UpdateCheckResult? LastResult
    {
        get
        {
            lock (_sync)
            {
                return _lastResult;
            }
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_manifestUrl))
        {
            return CacheResult(new UpdateCheckResult(
                false,
                GetCurrentVersion(),
                null,
                null,
                null,
                "Update manifest URL is not configured."));
        }

        try
        {
            using var response = await _httpClient.GetAsync(_manifestUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                return CacheResult(new UpdateCheckResult(
                    false,
                    GetCurrentVersion(),
                    null,
                    null,
                    null,
                    $"Update check failed ({(int)response.StatusCode})."));
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, JsonOptions);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                return CacheResult(new UpdateCheckResult(
                    false,
                    GetCurrentVersion(),
                    null,
                    null,
                    null,
                    "Update manifest is invalid."));
            }

            var currentVersion = ParseVersion(GetCurrentVersion());
            var latestVersion = ParseVersion(manifest.Version);
            if (currentVersion is null || latestVersion is null)
            {
                return CacheResult(new UpdateCheckResult(
                    false,
                    GetCurrentVersion(),
                    manifest.Version,
                    null,
                    null,
                    "Unable to parse version information."));
            }

            var platformKey = GetPlatformKey();
            manifest.Downloads.TryGetValue(platformKey, out var downloadUrl);

            var hasUpdate = latestVersion > currentVersion;
            return CacheResult(new UpdateCheckResult(
                hasUpdate,
                currentVersion.ToString(3),
                latestVersion.ToString(3),
                downloadUrl,
                manifest.ReleaseNotes ?? string.Empty,
                hasUpdate
                    ? null
                    : $"You're up to date ({currentVersion.ToString(3)})."));
        }
        catch (Exception ex)
        {
            return CacheResult(new UpdateCheckResult(
                false,
                GetCurrentVersion(),
                null,
                null,
                null,
                $"Update check failed: {ex.Message}"));
        }
    }

    public bool OpenDownloadUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetCurrentVersion()
    {
        var assembly = typeof(App).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var core = informational.Split('+', 2)[0];
            if (!string.IsNullOrWhiteSpace(core))
            {
                return core;
            }
        }

        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "0.0.0";
    }

    private UpdateCheckResult CacheResult(UpdateCheckResult result)
    {
        lock (_sync)
        {
            _lastResult = result;
            return result;
        }
    }

    private static string GetPlatformKey()
    {
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };

        if (OperatingSystem.IsWindows())
        {
            return $"win-{arch}";
        }

        if (OperatingSystem.IsMacOS())
        {
            return $"osx-{arch}";
        }

        return $"linux-{arch}";
    }

    private static Version? ParseVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var core = version.Split('+', 2)[0].Trim();
        return Version.TryParse(core, out var parsed) ? parsed : null;
    }
}

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? DownloadUrl,
    string? ReleaseNotes,
    string? Message);

public sealed class UpdateManifest
{
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public Dictionary<string, string> Downloads { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
