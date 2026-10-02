using GeoGen.DesktopApp.Services;
using NUnit.Framework;
using System.Text.Json;

namespace GeoGen.DesktopApp.Tests;

public sealed class PackagedDrawingTests
{
    private string _temporary = null!;
    private ToolLocator _tools = null!;
    private WorkspaceManager _workspaces = null!;
    private ProcessRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        var application = Environment.GetEnvironmentVariable("STUDIO_PACKAGE_APP");
        if (string.IsNullOrEmpty(application)) Assert.Ignore("Exercised against published executables in release CI.");
        if (Environment.GetEnvironmentVariable("STUDIO_PACKAGE_RID") != UpdateService.RuntimeIdentifier)
            Assert.Ignore("Cross-built executable cannot run on this host.");
        _temporary = Directory.CreateTempSubdirectory("Studio Drawing With Spaces ").FullName;
        _tools = new ToolLocator(application);
        _workspaces = new WorkspaceManager(_temporary);
        _runner = new ProcessRunner(TestContext.Progress.Write);
    }

    [TearDown]
    public void TearDown()
    {
        if (_temporary is not null && Directory.Exists(_temporary)) Directory.Delete(_temporary, recursive: true);
    }

    [Test]
    public async Task SingleFileDrawerStartsWithItsShippedConfigurationInAWritableWorkspace()
    {
        var drawer = _tools.FindDrawer();
        Assert.That(drawer, Is.Not.Null);
        var work = await _workspaces.PrepareFigureWorkspaceAsync(_workspaces.CreateRunWorkspace(), drawer!.WorkingDirectory, CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        // Closing stdin exercises initialization without requiring TeX or an interactive console.
        var result = await _runner.RunAsync(drawer.ExecutablePath, drawer.PrefixArguments, work, Array.Empty<string>(), timeout.Token);
        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.AllLines));
            Assert.That(result.Contains("drawing rule(s)"), Is.True);
            Assert.That(result.Contains(" FTL]"), Is.False);
            Assert.That(File.Exists(Path.Combine(work, "Logs", "logs.txt")), Is.True);
        });
    }

    [Test]
    public async Task ConfigurationGeneratesTheoremsAndExportsRealPdfFigures()
    {
        if (Environment.GetEnvironmentVariable("STUDIO_TEST_RENDER") != "1")
            Assert.Ignore("Real rendering is exercised on Windows, Linux, and macOS after drawing-tool setup.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var cancellation = timeout.Token;
        var engine = _tools.FindEngine();
        var drawer = _tools.FindDrawer();
        Assert.That(engine, Is.Not.Null);
        Assert.That(drawer, Is.Not.Null);
        var run = _workspaces.CreateRunWorkspace();
        // Use the engine's shipped exploration example, which produces drawable results.
        var input = await File.ReadAllTextAsync(Path.Combine(engine!.WorkingDirectory, "Examples", "Inputs", "input.txt"), cancellation);
        await _workspaces.PrepareEngineRunAsync(run, input, cancellation);
        var generated = await _runner.RunAsync(engine!.ExecutablePath,
            engine.PrefixArguments.Concat(new[] { Path.Combine(engine.WorkingDirectory, "settings.json"), run.SettingsFilePath }),
            engine.WorkingDirectory, null, cancellation);
        Assert.That(generated.ExitCode, Is.Zero, string.Join(Environment.NewLine, generated.AllLines));
        var json = Directory.GetFiles(run.JsonOutputDirectory, "*.json").Single();
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(json, cancellation));
        var count = Math.Min(2, document.RootElement.GetArrayLength());
        Assert.That(count, Is.GreaterThan(0));
        var work = await _workspaces.PrepareFigureWorkspaceAsync(run, drawer!.WorkingDirectory, cancellation);
        var drawn = await _runner.RunAsync(drawer.ExecutablePath, drawer.PrefixArguments, work, new[] { json, $"1-{count}" }, cancellation);
        Assert.Multiple(() =>
        {
            Assert.That(drawn.ExitCode, Is.Zero, string.Join(Environment.NewLine, drawn.AllLines));
            Assert.That(drawn.Contains(" FTL]"), Is.False, string.Join(Environment.NewLine, drawn.AllLines));
            Assert.That(drawn.Contains(" ERR]"), Is.False, string.Join(Environment.NewLine, drawn.AllLines));
        });
        var destination = Path.Combine(_temporary, "Exported Figures");
        var converted = await new FigureConverter(_runner, TestContext.Progress.Write).ConvertAsync(Path.Combine(work, "Data"), destination, cancellation);
        Assert.Multiple(() =>
        {
            Assert.That(converted.SourceCount, Is.EqualTo(count));
            Assert.That(converted.ConvertedCount, Is.EqualTo(count));
            Assert.That(converted.EpsFallbackCount, Is.Zero);
        });
        foreach (var pdf in Directory.GetFiles(destination, "*.pdf"))
        {
            await using var stream = File.OpenRead(pdf);
            var signature = new byte[5];
            await stream.ReadExactlyAsync(signature, cancellation);
            Assert.That(System.Text.Encoding.ASCII.GetString(signature), Is.EqualTo("%PDF-"));
        }
    }
}
