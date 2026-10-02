using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;

namespace GeoGen.DesktopApp.Services;

public sealed record PreparedUpdate(string Version, string RuntimeIdentifier, string DirectoryName,
    Dictionary<string, string> Files, bool Attempted = false);

/// <summary>Verified, side-by-side user updates. The installed application remains a recovery launcher.</summary>
public sealed class AutomaticUpdateService
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient _client;
    private readonly string _root;
    public AutomaticUpdateService(string? root = null, HttpClient? client = null)
    {
        _root = Path.GetFullPath(root ?? Path.Combine(Path.GetDirectoryName(StudioSettings.SettingsPath)!, "Updates"));
        _client = client ?? Client;
    }

    // Never run user-writable update code through an elevated Windows launcher.
    public static bool CanUpdate
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return true;
            using var identity = WindowsIdentity.GetCurrent();
            return !new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public PreparedUpdate? Pending => Read("pending.json");
    public bool HasFailedUpdate(string version) => Pending is { Attempted: true } pending && pending.Version == version;

    public async Task<PreparedUpdate> PrepareAsync(AvailableRelease release, IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var package = release.Package ?? throw new InvalidOperationException("This release does not include a verified update package.");
        ValidatePackage(release.Version, package);
        Directory.CreateDirectory(_root);
        using var updateLock = AcquireLock();
        if (Pending is { } pending && pending.Version == release.Version && Verify(pending))
        {
            pending = pending with { Attempted = false };
            Write("pending.json", pending);
            return pending;
        }
        var key = Guid.NewGuid().ToString("N");
        var temporary = Path.Combine(_root, "incoming-" + key);
        Directory.CreateDirectory(temporary);
        try
        {
            var archive = Path.Combine(temporary, "package");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(15));
            await DownloadAsync(package, archive, progress, timeout.Token);
            var unpacked = Path.Combine(temporary, "app");
            var hashes = await Task.Run(() => UpdateArchive.Extract(archive, unpacked, release.Version,
                package.RuntimeIdentifier, timeout.Token), timeout.Token);
            cancellationToken.ThrowIfCancellationRequested();
            var prepared = new PreparedUpdate(release.Version, package.RuntimeIdentifier, key, hashes);
            Directory.CreateDirectory(Path.Combine(_root, "versions"));
            Directory.Move(unpacked, VersionDirectory(prepared));
            Write("pending.json", prepared);
            return prepared;
        }
        finally { Directory.Delete(temporary, recursive: true); }
    }

    public bool TryLaunchLatest(string currentVersion, string rid, string[] arguments,
        Func<ProcessStartInfo, bool>? launch = null)
    {
        if ((!CanUpdate && launch is null) || arguments.Contains("--skip-cached-update", StringComparer.Ordinal)) return false;
        try
        {
            if (!Directory.Exists(_root)) return false;
            using var updateLock = AcquireLock();
            foreach (var (name, pending) in new[] { ("pending.json", true), ("active.json", false) })
            {
                var candidate = Read(name);
                if (candidate is null || candidate.RuntimeIdentifier != rid ||
                    !ReleaseVersion.IsNewer(candidate.Version, currentVersion) || pending && candidate.Attempted || !Verify(candidate)) continue;
                if (pending) Write(name, candidate with { Attempted = true });
                var info = new ProcessStartInfo
                {
                    FileName = Path.Combine(UpdateArchive.ApplicationDirectory(VersionDirectory(candidate), rid), UpdateArchive.ExecutableName(rid)),
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory
                };
                foreach (var argument in arguments) info.ArgumentList.Add(argument);
                if (launch?.Invoke(info) ?? Process.Start(info) is not null) return true;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidOperationException or System.ComponentModel.Win32Exception or JsonException) { }
        return false;
    }

    public void ConfirmStarted(string currentVersion, string rid, string applicationDirectory)
    {
        if (!CanUpdate || !Directory.Exists(_root)) return;
        try
        {
            using var updateLock = AcquireLock();
            if (Pending is not { } pending || pending.Version != currentVersion || pending.RuntimeIdentifier != rid ||
                !SamePath(UpdateArchive.ApplicationDirectory(VersionDirectory(pending), rid), applicationDirectory)) return;
            Write("active.json", pending with { Attempted = false });
            File.Delete(Path.Combine(_root, "pending.json"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { }
    }

    public bool Verify(PreparedUpdate update)
    {
        if (!ValidIdentity(update)) return false;
        try
        {
            var directory = VersionDirectory(update);
            if (new DirectoryInfo(_root).Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                new DirectoryInfo(Path.GetDirectoryName(directory)!).Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                new DirectoryInfo(directory).Attributes.HasFlag(FileAttributes.ReparsePoint)) return false;
            var directories = new Stack<string>();
            directories.Push(directory);
            var fileCount = 0;
            var entryCount = 0;
            while (directories.TryPop(out var current))
            {
                foreach (var path in Directory.EnumerateFileSystemEntries(current))
                {
                    var attributes = File.GetAttributes(path);
                    if (attributes.HasFlag(FileAttributes.ReparsePoint) || ++entryCount > 10_000) return false;
                    if (attributes.HasFlag(FileAttributes.Directory)) directories.Push(path);
                    else fileCount++;
                }
            }
            if (fileCount != update.Files.Count) return false;
            foreach (var (relative, hash) in update.Files)
            {
                if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(part => part is "" or "." or ".." || part.Contains(':')) || hash is null || hash.Length != 64) return false;
                using var stream = File.OpenRead(Path.Combine(directory, relative));
                if (!Convert.ToHexString(SHA256.HashData(stream)).Equals(hash, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return File.ReadAllText(Path.Combine(UpdateArchive.ApplicationDirectory(directory, update.RuntimeIdentifier), "studio-version.txt"))
                .Trim().Trim('\uFEFF') == update.Version;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }

    private static bool ValidIdentity(PreparedUpdate update) =>
        !string.IsNullOrEmpty(update.Version) && !string.IsNullOrEmpty(update.DirectoryName) && update.Files is not null &&
        ReleaseVersion.IsNewer(update.Version, "0.0.0") && IsRuntime(update.RuntimeIdentifier) &&
        update.DirectoryName.Length == 32 && update.DirectoryName.All(char.IsAsciiHexDigit) && update.Files.Count is > 0 and <= 10_000;
    private static bool IsRuntime(string rid) => rid is "win-x64" or "win-arm64" or "osx-x64" or "osx-arm64" or "linux-x64" or "linux-arm64";
    private static bool SamePath(string left, string right) => Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar)
        .Equals(Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private string VersionDirectory(PreparedUpdate update) => Path.Combine(_root, "versions", update.DirectoryName);
    private FileStream AcquireLock() => new(Path.Combine(_root, "update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    private PreparedUpdate? Read(string name)
    {
        try
        {
            var path = Path.Combine(_root, name);
            if (new FileInfo(path).Length > 2_097_152) return null;
            var update = JsonSerializer.Deserialize<PreparedUpdate>(File.ReadAllText(path));
            return update is not null && update.Files is not null && update.DirectoryName is not null && ValidIdentity(update) ? update : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { return null; }
    }
    private void Write(string name, PreparedUpdate update)
    {
        var temporary = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".tmp");
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(update)); File.Move(temporary, Path.Combine(_root, name), overwrite: true); }
        finally { File.Delete(temporary); }
    }
    private static void ValidatePackage(string version, UpdatePackage package)
    {
        var suffix = package.RuntimeIdentifier.StartsWith("linux-", StringComparison.Ordinal) ? ".tar.gz" : ".zip";
        if (!ReleaseVersion.IsNewer(version, "0.0.0") || !IsRuntime(package.RuntimeIdentifier) ||
            package.Url.AbsoluteUri != $"{AppInfo.RepositoryUrl}/releases/download/v{version}/PlanarGeometryStudio-v{version}-{package.RuntimeIdentifier}{suffix}" ||
            package.Size is <= 0 or > 536_870_912 || package.Sha256.Length != 64 || !package.Sha256.All(char.IsAsciiHexDigit))
            throw new InvalidDataException("The release does not have a valid update package.");
    }
    private async Task DownloadAsync(UpdatePackage package, string destination, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var url = package.Url;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            if (url.Scheme != "https" || url.Host is not ("github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com"))
                throw new InvalidDataException("An update download redirected outside GitHub.");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd($"PlanarGeometryStudio/{AppInfo.Version}");
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
            { url = location.IsAbsoluteUri ? location : new Uri(url, location); continue; }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is { } length && length != package.Size)
                throw new InvalidDataException("The update size differs from the published release.");
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[131_072];
            long total = 0;
            int count;
            while ((count = await source.ReadAsync(buffer, cancellationToken)) != 0)
            {
                total += count;
                if (total > package.Size) throw new InvalidDataException("The update exceeded its published size.");
                hash.AppendData(buffer, 0, count);
                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                progress?.Report((double)total / package.Size);
            }
            if (total != package.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(package.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update checksum did not match. The current application is unchanged.");
            return;
        }
        throw new HttpRequestException("Too many redirects while downloading the update.");
    }
}
