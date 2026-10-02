using GeoGen.DesktopApp.Services;
using NUnit.Framework;
using System.Diagnostics;
using System.Text.Json;

namespace GeoGen.DesktopApp.Tests;

public sealed class AutomaticPackageSmokeTests
{
    [Test]
    public async Task PublishedPackageCanBePreparedAndItsNativeApplicationCanStart()
    {
        var archive = Environment.GetEnvironmentVariable("STUDIO_UPDATE_ARCHIVE");
        if (string.IsNullOrEmpty(archive)) Assert.Ignore("Exercised after native packaging in release CI.");
        var rid = Environment.GetEnvironmentVariable("STUDIO_UPDATE_RID")!;
        var version = Environment.GetEnvironmentVariable("STUDIO_UPDATE_VERSION")!;
        var temporary = Directory.CreateTempSubdirectory("studio-native-update-").FullName;
        try
        {
            var bytes = await File.ReadAllBytesAsync(archive!);
            using var client = AutomaticUpdateTests.Client(bytes);
            var cache = Path.Combine(temporary, "Updates");
            var updater = new AutomaticUpdateService(cache, client);
            var prepared = await updater.PrepareAsync(AutomaticUpdateTests.Release(bytes, rid, version));
            Assert.That(updater.Verify(prepared), Is.True);
            if (rid != UpdateService.RuntimeIdentifier) return; // Windows ARM64 is cross-built on x64.
            var receipt = Path.Combine(temporary, "started.json");
            Process? process = null;
            Assert.That(updater.TryLaunchLatest("0.0.0", rid, new[] { "--updater-smoke", cache, receipt }, info =>
            { process = Process.Start(info); return process is not null; }), Is.True);
            using (process)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await process!.WaitForExitAsync(timeout.Token);
                Assert.That(process.ExitCode, Is.Zero);
            }
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(receipt));
            Assert.That(document.RootElement.GetProperty("Version").GetString(), Is.EqualTo(version));
            if (AutomaticUpdateService.CanUpdate)
            {
                Assert.That(updater.Pending, Is.Null, "Native startup must acknowledge the prepared update.");
                Assert.That(updater.TryLaunchLatest("0.0.0", rid, Array.Empty<string>(), _ => true), Is.True);
            }
        }
        finally { Directory.Delete(temporary, recursive: true); }
    }
}
