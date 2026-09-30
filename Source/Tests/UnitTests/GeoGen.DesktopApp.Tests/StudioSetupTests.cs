using GeoGen.DesktopApp.Services;
using NUnit.Framework;

namespace GeoGen.DesktopApp.Tests;

public sealed class StudioSetupTests
{
    private string _temporary = null!;

    [SetUp]
    public void SetUp() => _temporary = Directory.CreateTempSubdirectory("pgs-tests-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_temporary, recursive: true);

    [Test]
    public void PersistsPreferencesAtomicallyOutsideInstallDirectory()
    {
        var path = Path.Combine(_temporary, "profile", "settings.json");
        new StudioSettings { SetupCompleted = true, CheckForUpdatesOnLaunch = false }.Save(path);
        var loaded = StudioSettings.Load(path);
        Assert.Multiple(() =>
        {
            Assert.That(loaded.SetupCompleted, Is.True);
            Assert.That(loaded.CheckForUpdatesOnLaunch, Is.False);
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"), Is.Empty);
        });
    }

    [TestCase("corrupt json")]
    [TestCase("null")]
    public void CorruptPreferencesDoNotPreventStartup(string content)
    {
        var path = Path.Combine(_temporary, "settings.json");
        File.WriteAllText(path, content);
        var loaded = StudioSettings.Load(path);
        Assert.That(loaded.SetupCompleted, Is.False);
        Assert.That(loaded.CheckForUpdatesOnLaunch, Is.True);
    }

    [Test]
    public void DiscoversToolsInPathsWithSpacesWithoutRequiringPathMutation()
    {
        var directory = Directory.CreateDirectory(Path.Combine(_temporary, "TeX tools", "bin")).FullName;
        var filename = OperatingSystem.IsWindows() ? "mpost.exe" : "mpost";
        File.WriteAllText(Path.Combine(directory, filename), "test");
        Assert.That(DrawingToolEnvironment.FindExecutableIn(new[] { directory }, "mpost"), Is.EqualTo(Path.Combine(directory, filename)));
    }

    [Test]
    public void TermsAreIncludedInExecutable()
    {
        using var stream = typeof(StudioSettings).Assembly.GetManifestResourceStream("GeoGen.DesktopApp.Terms.md");
        Assert.That(stream, Is.Not.Null);
        using var reader = new StreamReader(stream!);
        Assert.That(reader.ReadToEnd(), Does.Contain("do not add restrictions"));
    }
}
