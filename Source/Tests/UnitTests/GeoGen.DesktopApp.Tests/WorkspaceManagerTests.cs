using GeoGen.DesktopApp.Services;
using NUnit.Framework;
using System.Text.Json;

namespace GeoGen.DesktopApp.Tests;

public sealed class WorkspaceManagerTests
{
    private string _rootDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "PlanarGeometryStudio.Tests", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    [Test]
    public async Task CreatesAnIsolatedRunWithWritableEngineSettings()
    {
        var manager = new WorkspaceManager(_rootDirectory);
        var workspace = manager.CreateRunWorkspace();

        await manager.PrepareEngineRunAsync(workspace, "sample input", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(Path.Combine(workspace.InputDirectory, "input.txt")), Is.EqualTo("sample input"));
            Assert.That(Directory.Exists(workspace.JsonOutputDirectory), Is.True);
            Assert.That(File.Exists(workspace.SettingsFilePath), Is.True);
        });

        using var settings = JsonDocument.Parse(File.ReadAllText(workspace.SettingsFilePath));
        var inputDirectory = settings.RootElement
            .GetProperty("ProblemGeneratorInputProviderSettings")
            .GetProperty("InputFolderPath")
            .GetString();

        Assert.That(inputDirectory, Is.EqualTo(workspace.InputDirectory));
    }

    [Test]
    public void CreatesUniqueRunsAndFindsTheLatest()
    {
        var manager = new WorkspaceManager(_rootDirectory);
        var first = manager.CreateRunWorkspace();
        var second = manager.CreateRunWorkspace();

        Assert.Multiple(() =>
        {
            Assert.That(second.RootDirectory, Is.Not.EqualTo(first.RootDirectory));
            Assert.That(manager.FindLatestRun()?.RootDirectory, Is.EqualTo(second.RootDirectory));
        });
    }
}
