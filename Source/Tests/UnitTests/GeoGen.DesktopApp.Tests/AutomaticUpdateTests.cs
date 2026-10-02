using GeoGen.DesktopApp.Services;
using NUnit.Framework;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GeoGen.DesktopApp.Tests;

public sealed class AutomaticUpdateTests
{
    private string _directory = null!;
    [SetUp] public void SetUp() => _directory = Directory.CreateTempSubdirectory("studio-update-test-").FullName;
    [TearDown] public void TearDown() => Directory.Delete(_directory, recursive: true);

    [Test]
    public void UpdatePreferencesUseAnAbsoluteUserPathAndPreserveExistingCheckChoices()
    {
        Assert.That(Path.IsPathRooted(StudioSettings.SettingsPath), Is.True);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "{\"CheckForUpdatesOnLaunch\":false}");
        var settings = StudioSettings.Load(path);
        Assert.That(settings.CheckForUpdatesOnLaunch, Is.False);
        settings.DownloadUpdatesAutomatically = false;
        settings.Save(path);
        Assert.That(StudioSettings.Load(path).DownloadUpdatesAutomatically, Is.False);
    }

    [TestCase("win-x64")]
    [TestCase("win-arm64")]
    [TestCase("linux-x64")]
    [TestCase("linux-arm64")]
    [TestCase("osx-x64")]
    [TestCase("osx-arm64")]
    public async Task PreparesVerifiedArchiveAndKeepsTheInstalledCopySeparate(string rid)
    {
        var bytes = Archive(rid);
        using var client = Client(bytes);
        var service = new AutomaticUpdateService(Path.Combine(_directory, "Updates"), client);
        var prepared = await service.PrepareAsync(Release(bytes, rid));
        Assert.Multiple(() =>
        {
            Assert.That(service.Pending, Is.Not.Null);
            Assert.That(service.Verify(prepared), Is.True);
            Assert.That(prepared.Attempted, Is.False);
            Assert.That(Directory.EnumerateDirectories(Path.Combine(_directory, "Updates"), "incoming-*"), Is.Empty);
        });
        ProcessStartInfo? launch = null;
        Assert.That(service.TryLaunchLatest("1.2.4", rid, new[] { "--setup" }, info => { launch = info; return true; }), Is.True);
        Assert.That(launch!.ArgumentList, Is.EqualTo(new[] { "--setup" }));
        Assert.That(launch.FileName, Does.StartWith(Path.Combine(_directory, "Updates", "versions")));
        Assert.That(service.Pending!.Attempted, Is.True);
        Assert.That(service.TryLaunchLatest("1.2.4", rid, Array.Empty<string>(), _ => throw new Exception("Failed update must not be tried twice")), Is.False);
        Assert.That(service.TryLaunchLatest("1.2.5", rid, Array.Empty<string>(), _ => throw new Exception("Same version must not redirect")), Is.False);
        var reused = await service.PrepareAsync(Release(bytes, rid));
        Assert.That(reused.DirectoryName, Is.EqualTo(prepared.DirectoryName));
        Assert.That(reused.Attempted, Is.False);
        var app = Path.GetDirectoryName(launch.FileName)!;
        service.ConfirmStarted("1.2.5", rid, app);
        if (AutomaticUpdateService.CanUpdate)
        {
            Assert.That(service.Pending, Is.Null);
            Assert.That(service.TryLaunchLatest("1.2.4", rid, Array.Empty<string>(), _ => true), Is.True);
        }
    }

    [Test]
    public async Task FailedNewStartupFallsBackToThePreviouslyAcknowledgedVersion()
    {
        if (!AutomaticUpdateService.CanUpdate) Assert.Ignore("Elevated launchers deliberately bypass cached apps.");
        var root = Path.Combine(_directory, "Updates");
        var firstBytes = Archive("linux-x64");
        using var firstClient = Client(firstBytes);
        var first = new AutomaticUpdateService(root, firstClient);
        var prepared = await first.PrepareAsync(Release(firstBytes, "linux-x64"));
        first.ConfirmStarted("1.2.5", "linux-x64", UpdateArchive.ApplicationDirectory(Path.Combine(root, "versions", prepared.DirectoryName), "linux-x64"));
        var secondBytes = Archive("linux-x64", "1.2.6");
        using var secondClient = Client(secondBytes);
        var second = new AutomaticUpdateService(root, secondClient);
        var pending = await second.PrepareAsync(Release(secondBytes, "linux-x64", "1.2.6"));
        var launches = new List<string>();
        Assert.That(second.TryLaunchLatest("1.2.4", "linux-x64", Array.Empty<string>(), info =>
        {
            launches.Add(info.FileName);
            return launches.Count == 2;
        }), Is.True);
        Assert.That(launches[0], Does.Contain(pending.DirectoryName));
        Assert.That(launches[1], Does.Contain(prepared.DirectoryName));
        Assert.That(second.HasFailedUpdate("1.2.6"), Is.True);
    }

    [Test]
    public async Task CorruptOrExtraCachedFilesNeverLaunch()
    {
        var bytes = Archive("win-x64");
        using var client = Client(bytes);
        var service = new AutomaticUpdateService(Path.Combine(_directory, "Updates"), client);
        var prepared = await service.PrepareAsync(Release(bytes, "win-x64"));
        var directory = Path.Combine(_directory, "Updates", "versions", prepared.DirectoryName);
        File.WriteAllText(Path.Combine(directory, "unexpected.dll"), "tampered");
        Assert.That(service.Verify(prepared), Is.False);
        File.Delete(Path.Combine(directory, "unexpected.dll"));
        File.AppendAllText(Path.Combine(UpdateArchive.ApplicationDirectory(directory, "win-x64"), "PlanarGeometryStudio.exe"), "tampered");
        Assert.That(service.TryLaunchLatest("1.2.4", "win-x64", Array.Empty<string>(), _ => throw new Exception("Corrupt update launched")), Is.False);
    }

    [Test]
    public async Task WrongChecksumLeavesTheExistingUpdateUnchanged()
    {
        var bytes = Archive("win-x64");
        using var client = Client(bytes);
        var service = new AutomaticUpdateService(Path.Combine(_directory, "Updates"), client);
        var good = await service.PrepareAsync(Release(bytes, "win-x64"));
        var bad = Release(bytes, "win-x64", "1.2.6") with { Package = Release(bytes, "win-x64", "1.2.6").Package! with { Sha256 = new string('0', 64) } };
        Assert.ThrowsAsync<InvalidDataException>(async () => await service.PrepareAsync(bad));
        Assert.That(service.Pending!.DirectoryName, Is.EqualTo(good.DirectoryName));
        Assert.That(service.Verify(good), Is.True);
    }

    [Test]
    public void RejectsUnexpectedDownloadLocationsAndCancellation()
    {
        var bytes = Archive("win-x64");
        using var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        { Headers = { Location = new Uri("https://example.org/payload.zip") } }));
        var service = new AutomaticUpdateService(Path.Combine(_directory, "Updates"), client);
        Assert.ThrowsAsync<InvalidDataException>(async () => await service.PrepareAsync(Release(bytes, "win-x64")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var cancelledClient = Client(bytes);
        var cancelled = new AutomaticUpdateService(Path.Combine(_directory, "Cancelled"), cancelledClient);
        Assert.CatchAsync<OperationCanceledException>(async () => await cancelled.PrepareAsync(Release(bytes, "win-x64"), cancellationToken: cancellation.Token));
        Assert.That(cancelled.Pending, Is.Null);
    }

    [TestCase("../escaped")]
    [TestCase("PlanarGeometryStudio/../../escaped")]
    [TestCase("/absolute")]
    [TestCase("PlanarGeometryStudio/unsafe\\file")]
    [TestCase("PlanarGeometryStudio/file:stream")]
    public void RejectsUnsafeArchivePaths(string name)
    {
        var archive = Path.Combine(_directory, "unsafe.zip");
        using (var stream = File.Create(archive))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            zip.CreateEntry(name);
        Assert.Throws<InvalidDataException>(() => UpdateArchive.Extract(archive, Path.Combine(_directory, "Extracted"), "1.2.5", "win-x64"));
        Assert.That(File.Exists(Path.Combine(_directory, "escaped")), Is.False);
    }

    [Test]
    public void RejectsSymbolicLinksInBothArchiveFormats()
    {
        var zipPath = Path.Combine(_directory, "link.zip");
        using (var stream = File.Create(zipPath))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            zip.CreateEntry("PlanarGeometryStudio/link").ExternalAttributes = 0xa1ff << 16;
        Assert.Throws<InvalidDataException>(() => UpdateArchive.Extract(zipPath, Path.Combine(_directory, "Zip"), "1.2.5", "win-x64"));
        var tarPath = Path.Combine(_directory, "link.tar.gz");
        using (var stream = File.Create(tarPath))
        using (var gzip = new GZipStream(stream, CompressionMode.Compress))
        using (var tar = new TarWriter(gzip))
            tar.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink, "PlanarGeometryStudio/link") { LinkName = "../../outside" });
        Assert.Throws<InvalidDataException>(() => UpdateArchive.Extract(tarPath, Path.Combine(_directory, "Tar"), "1.2.5", "linux-x64"));
    }

    [Test]
    public void RejectsDuplicateArchivePathsAndMismatchedVersions()
    {
        var duplicate = Path.Combine(_directory, "duplicate.zip");
        using (var stream = File.Create(duplicate))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            zip.CreateEntry("PlanarGeometryStudio/a");
            zip.CreateEntry("PlanarGeometryStudio/a");
        }
        Assert.Throws<InvalidDataException>(() => UpdateArchive.Extract(duplicate, Path.Combine(_directory, "Duplicate"), "1.2.5", "win-x64"));
        var wrongVersion = Path.Combine(_directory, "version.zip");
        File.WriteAllBytes(wrongVersion, Archive("win-x64"));
        Assert.Throws<InvalidDataException>(() => UpdateArchive.Extract(wrongVersion, Path.Combine(_directory, "Version"), "1.2.6", "win-x64"));
    }

    [Test]
    public void RejectsMalformedStateWithoutLeavingTheRecoveryLauncher()
    {
        var root = Path.Combine(_directory, "Updates");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "pending.json"), "{\"Version\":null,\"RuntimeIdentifier\":null,\"DirectoryName\":null,\"Files\":null}");
        Assert.That(new AutomaticUpdateService(root).TryLaunchLatest("1.2.4", "win-x64", Array.Empty<string>(), _ => throw new Exception("Malformed update launched")), Is.False);
    }

    [Test]
    public async Task UpdateDiscoveryRequiresThePublishedDigestAndExactPortableAsset()
    {
        var name = "PlanarGeometryStudio-v1.2.5-win-x64.zip";
        var url = "https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/download/v1.2.5/" + name;
        foreach (var digest in new[] { "sha256:" + new string('a', 64), "", "sha256:bad" })
        {
            var json = JsonSerializer.Serialize(new { tag_name = "v1.2.5", draft = false, prerelease = false,
                assets = new[] { new { name, browser_download_url = url, size = 100L, digest, state = "uploaded" } } });
            using var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }));
            var result = await new UpdateService(client).CheckAsync("1.2.4", "win-x64");
            Assert.That(result.Release!.Package is not null, Is.EqualTo(digest.Length == 71));
        }
    }

    internal static AvailableRelease Release(byte[] bytes, string rid, string version = "1.2.5") => new(version,
        new Uri($"https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/tag/v{version}"), null,
        new UpdatePackage(rid, new Uri($"https://github.com/nikolaveselinov/Planar-Geometry-Studio/releases/download/v{version}/PlanarGeometryStudio-v{version}-{rid}" +
            (rid.StartsWith("linux-", StringComparison.Ordinal) ? ".tar.gz" : ".zip")), bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes))));
    internal static HttpClient Client(byte[] bytes) => new(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }));
    private static byte[] Archive(string rid, string version = "1.2.5")
    {
        var prefix = rid.StartsWith("osx-", StringComparison.Ordinal) ? "Planar Geometry Studio.app/Contents/MacOS/" : "PlanarGeometryStudio/";
        var windows = rid.StartsWith("win-", StringComparison.Ordinal);
        var files = new Dictionary<string, string>
        {
            [prefix + UpdateArchive.ExecutableName(rid)] = "application",
            [prefix + "tools/engine/GeoGen" + (windows ? ".exe" : "")] = "engine",
            [prefix + "tools/drawer/GeoGen.DrawingLauncher" + (windows ? ".exe" : "")] = "drawer",
            [prefix + "TERMS.md"] = "Terms",
            [prefix + "studio-version.txt"] = version + "\n"
        };
        using var stream = new MemoryStream();
        if (rid.StartsWith("linux-", StringComparison.Ordinal))
        {
            using var gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true);
            using var tar = new TarWriter(gzip, leaveOpen: true);
            foreach (var (name, content) in files)
                tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)), Mode = (UnixFileMode)493 });
        }
        else
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var (name, content) in files)
            {
                var entry = zip.CreateEntry(name);
                entry.ExternalAttributes = 0x81ed << 16;
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        return stream.ToArray();
    }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(respond(request)); }
    }
}
