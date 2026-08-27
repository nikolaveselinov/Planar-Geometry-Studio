using GeoGen.DesktopApp.Services;
using NUnit.Framework;

namespace GeoGen.DesktopApp.Tests;

public sealed class ToolLocatorTests
{
    private string _rootDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "PlanarGeometryStudio.ToolLocator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    [Test]
    public void FindsBundledEngine()
    {
        var baseDirectory = Path.Combine(_rootDirectory, "app");
        var engineDirectory = Path.Combine(baseDirectory, "tools", "engine");
        Directory.CreateDirectory(engineDirectory);
        var engineDll = Path.Combine(engineDirectory, "GeoGen.dll");
        File.WriteAllText(engineDll, string.Empty);

        var location = new ToolLocator(baseDirectory).FindEngine();

        Assert.Multiple(() =>
        {
            Assert.That(location, Is.Not.Null);
            Assert.That(location!.ExecutablePath, Is.EqualTo("dotnet"));
            Assert.That(location.WorkingDirectory, Is.EqualTo(engineDirectory));
            Assert.That(location.PrefixArguments, Is.EqualTo(new[] { engineDll }));
        });
    }

    [Test]
    public void FindsDevelopmentEngineFromRidSpecificOutputDirectory()
    {
        var sourceDirectory = Path.Combine(_rootDirectory, "Source");
        var baseDirectory = Path.Combine(
            sourceDirectory,
            "Launchers",
            "GeoGen.DesktopApp",
            "bin",
            "Debug",
            "net10.0",
            "linux-x64");
        var engineDirectory = Path.Combine(
            sourceDirectory,
            "Launchers",
            "GeoGen.MainLauncher",
            "bin",
            "Debug",
            "net10.0");
        Directory.CreateDirectory(baseDirectory);
        Directory.CreateDirectory(engineDirectory);
        var engineDll = Path.Combine(engineDirectory, "GeoGen.dll");
        File.WriteAllText(engineDll, string.Empty);

        var location = new ToolLocator(baseDirectory).FindEngine();

        Assert.Multiple(() =>
        {
            Assert.That(location, Is.Not.Null);
            Assert.That(location!.ExecutablePath, Is.EqualTo("dotnet"));
            Assert.That(location.WorkingDirectory, Is.EqualTo(engineDirectory));
            Assert.That(location.PrefixArguments, Is.EqualTo(new[] { engineDll }));
        });
    }

    [Test]
    public void PrefersBundledEngineOverDevelopmentBuild()
    {
        var sourceDirectory = Path.Combine(_rootDirectory, "Source");
        var baseDirectory = Path.Combine(sourceDirectory, "Launchers", "GeoGen.DesktopApp", "bin", "Debug", "net10.0");
        var bundledDirectory = Path.Combine(baseDirectory, "tools", "engine");
        var developmentDirectory = Path.Combine(sourceDirectory, "Launchers", "GeoGen.MainLauncher", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(bundledDirectory);
        Directory.CreateDirectory(developmentDirectory);
        var bundledDll = Path.Combine(bundledDirectory, "GeoGen.dll");
        File.WriteAllText(bundledDll, string.Empty);
        File.WriteAllText(Path.Combine(developmentDirectory, "GeoGen.dll"), string.Empty);

        var location = new ToolLocator(baseDirectory).FindEngine();

        Assert.That(location?.PrefixArguments, Is.EqualTo(new[] { bundledDll }));
    }

    [Test]
    public void ReturnsNullWhenEngineIsMissing()
    {
        var baseDirectory = Path.Combine(_rootDirectory, "app");
        Directory.CreateDirectory(baseDirectory);

        var location = new ToolLocator(baseDirectory).FindEngine();

        Assert.That(location, Is.Null);
    }
}
