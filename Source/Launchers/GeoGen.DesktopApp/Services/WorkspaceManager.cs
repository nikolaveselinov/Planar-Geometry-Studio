using GeoGen.DesktopApp.Models;
using System.Globalization;
using System.Text.Json;

namespace GeoGen.DesktopApp.Services;

public sealed class WorkspaceManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public WorkspaceManager(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory ?? ResolveDefaultRootDirectory();
    }

    public string RootDirectory { get; }

    public string RunsDirectory => Path.Combine(RootDirectory, "Runs");

    public GenerationWorkspace CreateRunWorkspace()
    {
        var runDirectory = CreateTimestampedDirectory(RunsDirectory);
        var workspace = new GenerationWorkspace(runDirectory);
        CreateWorkspaceDirectories(workspace);
        return workspace;
    }

    public GenerationWorkspace? FindLatestRun()
    {
        if (!Directory.Exists(RunsDirectory))
            return null;

        var directory = new DirectoryInfo(RunsDirectory)
            .EnumerateDirectories()
            .OrderByDescending(item => item.Name, StringComparer.Ordinal)
            .FirstOrDefault();

        return directory is null ? null : new GenerationWorkspace(directory.FullName);
    }

    public async Task PrepareEngineRunAsync(
        GenerationWorkspace workspace,
        string input,
        CancellationToken cancellationToken)
    {
        CreateWorkspaceDirectories(workspace);

        await File.WriteAllTextAsync(
            Path.Combine(workspace.InputDirectory, "input.txt"),
            input,
            cancellationToken);

        var settings = new
        {
            Serilog = new
            {
                Using = new[]
                {
                    "Serilog.Sinks.Console",
                    "Serilog.Sinks.File",
                    "Serilog.Expressions"
                },
                MinimumLevel = "Information",
                WriteTo = new object[]
                {
                    new { Name = "Console" },
                    new
                    {
                        Name = "File",
                        Args = new { Path = Path.Combine(workspace.LogsDirectory, "engine.log") }
                    },
                    new
                    {
                        Name = "File",
                        Args = new
                        {
                            Path = Path.Combine(workspace.LogsDirectory, "problems.log"),
                            RestrictedToMinimumLevel = "Warning"
                        }
                    }
                }
            },
            ProblemGeneratorInputProviderSettings = new
            {
                InputFolderPath = workspace.InputDirectory,
                InputFilePrefix = "input",
                FileExtension = "txt"
            },
            ProblemGenerationRunnerSettings = new
            {
                ReadableOutputWithoutProofsFolder = workspace.ReadableOutputDirectory,
                WriteReadableOutputWithoutProofs = true,
                ReadableOutputWithProofsFolder = workspace.ReadableOutputWithProofsDirectory,
                WriteReadableOutputWithProofs = true,
                JsonOutputFolder = workspace.JsonOutputDirectory,
                WriteJsonOutput = true,
                OutputFilePrefix = "output",
                FileExtension = "txt",
                ReadableBestTheoremFolder = workspace.ReadableBestTheoremsDirectory,
                WriteReadableBestTheorems = true,
                JsonBestTheoremFolder = workspace.JsonBestTheoremsDirectory,
                WriteJsonBestTheorems = true,
                WriteBestTheoremsContinuously = false,
                BestTheoremsRewrittingIntervalInSeconds = 5,
                InferenceRuleUsageFilePath = Path.Combine(workspace.LogsDirectory, "inference-rule-usage.txt"),
                WriteInferenceRuleUsages = false,
                ProgressLoggingFrequency = 5,
                LogProgress = true
            },
            GeometryFailureTracerSettings = new
            {
                FailureFilePath = Path.Combine(workspace.LogsDirectory, "geometry-failures.txt"),
                LogFailures = true
            },
            InvalidInferenceTracerSettings = new
            {
                InvalidInferenceFolder = Path.Combine(workspace.LogsDirectory, "InvalidInferences"),
                FileExtension = "txt",
                MaximalNumberOfInvalidInferencesPerFile = 20
            },
            SortingGeometryFailureTracerSettings = new
            {
                FailureFilePath = Path.Combine(workspace.LogsDirectory, "sorting-geometry-failures.txt"),
                LogFailures = true
            }
        };

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(workspace.SettingsFilePath, json, cancellationToken);
    }

    public async Task<string> PrepareFigureWorkspaceAsync(
        GenerationWorkspace runWorkspace,
        string drawerDirectory,
        CancellationToken cancellationToken)
    {
        var figureRoot = CreateTimestampedDirectory(Path.Combine(runWorkspace.RootDirectory, "Figures"));
        var figureDataDirectory = Path.Combine(figureRoot, "Data");
        Directory.CreateDirectory(figureDataDirectory);

        var sourceDataDirectory = Path.Combine(drawerDirectory, "Data");
        if (!Directory.Exists(sourceDataDirectory))
            throw new DirectoryNotFoundException($"Drawing resources were not found at '{sourceDataDirectory}'.");

        await CopyDirectoryAsync(sourceDataDirectory, figureDataDirectory, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var settingsSource = Path.Combine(drawerDirectory, "settings.json");
        if (!File.Exists(settingsSource))
            throw new FileNotFoundException("The drawing settings file is missing.", settingsSource);

        File.Copy(settingsSource, Path.Combine(figureRoot, "settings.json"), overwrite: true);
        return figureRoot;
    }

    private static void CreateWorkspaceDirectories(GenerationWorkspace workspace)
    {
        Directory.CreateDirectory(workspace.InputDirectory);
        Directory.CreateDirectory(workspace.LogsDirectory);
        Directory.CreateDirectory(workspace.JsonOutputDirectory);
        Directory.CreateDirectory(workspace.ReadableOutputDirectory);
        Directory.CreateDirectory(workspace.ReadableOutputWithProofsDirectory);
        Directory.CreateDirectory(workspace.ReadableBestTheoremsDirectory);
        Directory.CreateDirectory(workspace.JsonBestTheoremsDirectory);
    }

    private static string CreateTimestampedDirectory(string parentDirectory)
    {
        Directory.CreateDirectory(parentDirectory);

        var stem = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var directory = Path.Combine(parentDirectory, stem);
        var suffix = 2;
        while (Directory.Exists(directory))
            directory = Path.Combine(parentDirectory, $"{stem}-{suffix++}");

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task CopyDirectoryAsync(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationFile = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            await using var source = File.OpenRead(sourceFile);
            await using var destination = File.Create(destinationFile);
            await source.CopyToAsync(destination, cancellationToken);
        }
    }

    private static string ResolveDefaultRootDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents))
            return Path.Combine(documents, "Planar Geometry Studio");

        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localData))
            return Path.Combine(localData, "PlanarGeometryStudio");

        return Path.Combine(Path.GetTempPath(), "PlanarGeometryStudio");
    }
}
