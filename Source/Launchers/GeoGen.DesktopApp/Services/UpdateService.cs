using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GeoGen.DesktopApp.Services;

public sealed record UpdatePackage(string RuntimeIdentifier, Uri Url, long Size, string Sha256);
public sealed record AvailableRelease(string Version, Uri ReleasePage, Uri? InstallerUrl, UpdatePackage? Package = null);
public sealed record UpdateCheckResult(string Message, AvailableRelease? Release = null);

public sealed class UpdateService
{
    private const int MaximumResponseBytes = 1_048_576;
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false });
    private readonly HttpClient _client;

    public UpdateService(HttpClient? client = null) => _client = client ?? Client;

    public async Task<UpdateCheckResult> CheckAsync(
        string currentVersion, string runtimeIdentifier, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/repos/nikolaveselinov/Planar-Geometry-Studio/releases/latest");
            request.Headers.UserAgent.ParseAdd($"PlanarGeometryStudio/{currentVersion}");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new("No stable release is available yet.");
            if (!response.IsSuccessStatusCode)
                return new("Update check unavailable. Try again later.");

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var content = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, timeout.Token)) != 0)
            {
                if (content.Length + count > MaximumResponseBytes)
                    return new("Update response was too large.");
                content.Write(buffer, 0, count);
            }

            using var document = JsonDocument.Parse(content.ToArray());
            return ParseRelease(document.RootElement, currentVersion, runtimeIdentifier);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new("Update check timed out. You can keep working offline.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or FormatException)
        {
            return new("Could not check for updates. You can keep working offline.");
        }
    }

    public static string RuntimeIdentifier
    {
        get
        {
            var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
            var architecture = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            return $"{platform}-{architecture}";
        }
    }

    private static UpdateCheckResult ParseRelease(JsonElement root, string current, string rid)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("tag_name", out var tag) || tag.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("draft", out var draft) || draft.ValueKind != JsonValueKind.False ||
            !root.TryGetProperty("prerelease", out var prerelease) || prerelease.ValueKind != JsonValueKind.False)
            return new("No stable update is available.");

        var version = tag.GetString()!;
        if (!ReleaseVersion.IsNewer(version, current))
            return new($"Studio {current} is up to date.");

        var tagSegment = Uri.EscapeDataString(version);
        var page = new Uri($"{AppInfo.RepositoryUrl}/releases/tag/{tagSegment}");
        Uri? installer = null;
        UpdatePackage? package = null;
        var suffix = rid.StartsWith("win-", StringComparison.Ordinal) ? "-setup.exe"
            : rid.StartsWith("osx-", StringComparison.Ordinal) ? ".pkg" : ".run";
        var archiveName = $"PlanarGeometryStudio-v{version.TrimStart('v')}-{rid}" +
            (rid.StartsWith("linux-", StringComparison.Ordinal) ? ".tar.gz" : ".zip");
        var expectedName = $"PlanarGeometryStudio-v{version.TrimStart('v')}-{rid}{suffix}";
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.ValueKind != JsonValueKind.Object ||
                    !asset.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String ||
                    !asset.TryGetProperty("browser_download_url", out var url) || url.ValueKind != JsonValueKind.String)
                    continue;

                var expectedUrl = $"{AppInfo.RepositoryUrl}/releases/download/{tagSegment}/{expectedName}";
                if (name.GetString() == expectedName && url.GetString() == expectedUrl)
                    installer = new Uri(expectedUrl);
                var archiveUrl = $"{AppInfo.RepositoryUrl}/releases/download/{tagSegment}/{archiveName}";
                if (name.GetString() == archiveName && url.GetString() == archiveUrl &&
                    asset.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.String && state.GetString() == "uploaded" &&
                    asset.TryGetProperty("size", out var size) && size.ValueKind == JsonValueKind.Number && size.TryGetInt64(out var bytes) && bytes is > 0 and <= 536_870_912 &&
                    asset.TryGetProperty("digest", out var digest) && digest.ValueKind == JsonValueKind.String &&
                    digest.GetString() is { } hash && hash.StartsWith("sha256:", StringComparison.Ordinal) &&
                    hash.Length == 71 && hash[7..].All(char.IsAsciiHexDigit))
                    package = new UpdatePackage(rid, new Uri(archiveUrl), bytes, hash[7..]);
            }
        }

        return new($"Studio {version.TrimStart('v')} is available.", new(version.TrimStart('v'), page, installer, package));
    }
}
