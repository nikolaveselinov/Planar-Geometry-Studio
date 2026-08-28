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

    [Test]
    public async Task CreatesUniqueRunsAcrossConcurrentInstances()
    {
        const int runCount = 64;
        using var start = new ManualResetEventSlim();

        var tasks = Enumerable.Range(0, runCount)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return new WorkspaceManager(_rootDirectory).CreateRunWorkspace().RootDirectory;
            }))
            .ToArray();

        start.Set();
        var directories = await Task.WhenAll(tasks);

        Assert.That(directories, Is.Unique);
    }

    [Test]
    public async Task CreatesIsolatedFigureWorkspaces()
    {
        var manager = new WorkspaceManager(_rootDirectory);
        var run = manager.CreateRunWorkspace();
        var drawerDirectory = Path.Combine(_rootDirectory, "drawer");
        var dataDirectory = Path.Combine(drawerDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);
        await File.WriteAllTextAsync(Path.Combine(dataDirectory, "drawing_rules.txt"), "rules");
        await File.WriteAllTextAsync(Path.Combine(drawerDirectory, "settings.json"), "{}");

        var first = await manager.PrepareFigureWorkspaceAsync(run, drawerDirectory, CancellationToken.None);
        var second = await manager.PrepareFigureWorkspaceAsync(run, drawerDirectory, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(File.ReadAllText(Path.Combine(first, "Data", "drawing_rules.txt")), Is.EqualTo("rules"));
            Assert.That(File.Exists(Path.Combine(second, "settings.json")), Is.True);
        });
    }
}
