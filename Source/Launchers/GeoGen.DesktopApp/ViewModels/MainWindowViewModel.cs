using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GeoGen.DesktopApp.Helpers;
using GeoGen.DesktopApp.Models;
using GeoGen.DesktopApp.Services;
using GeoGen.DesktopApp.Views;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace GeoGen.DesktopApp.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int FlushIntervalMilliseconds = 120;
    private const int MaximumOutputLength = 500_000;

    private static readonly FilePickerFileType[] ConfigurationFileTypes =
    {
        new("GeoGen input") { Patterns = new[] { "*.txt" } },
        new("All files") { Patterns = new[] { "*" } }
    };

    private const string DefaultInputText =
        """
        Constructions:

         Centroid
         CircleWithCenterThroughPoint
         CircleWithDiameter
         CircleWithRadius
         Circumcenter
         Circumcircle
         Excenter
         Excircle
         ExternalAngleBisector
         Incenter
         Incircle
         InternalAngleBisector
         IntersectionOfLineAndLineFromPoints
         IntersectionOfLines
         IntersectionOfLinesFromPoints
         IsoscelesTrapezoidPoint
         LineThroughCircumcenter
         Median
         Midline
         Midpoint
         MidpointOfArc
         MidpointOfOppositeArc
         NinePointCircle
         OppositePointOnCircumcircle
         Orthocenter
         ParallelLine
         ParallelLineToLineFromPoints
         ParallelogramPoint
         PerpendicularBisector
         PerpendicularLine
         PerpendicularLineAtPointOfLine
         PerpendicularLineToLineFromPoints
         PerpendicularProjection
         PerpendicularProjectionOnLineFromPoints
         PointReflection
         ReflectionInLine
         ReflectionInLineFromPoints
         SecondIntersectionOfCircleAndLineFromPoints
         SecondIntersectionOfTwoCircumcircles
         TangentLine

        Initial configuration:

         Triangle: A, B, C
         D = Circumcenter(A, B, C)
         E = Incenter(A, B, C)
         F = Orthocenter(A, B, C)

        Iterations: 1
        MaximalPoints: 4
        MaximalLines: 4
        MaximalCircles: 3
        SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric
        """;

    private readonly Window _window;
    private readonly WorkspaceManager _workspaceManager;
    private readonly ToolLocator _toolLocator;
    private readonly ProcessRunner _processRunner;
    private readonly FigureConverter _figureConverter;
    private readonly StringBuilder _outputBuffer = new();
    private readonly object _bufferLock = new();

    private CancellationTokenSource? _operationCancellation;
    private DispatcherTimer? _flushTimer;
    private GenerationWorkspace? _latestWorkspace;
    private string _savedInputText = DefaultInputText;
    private string _inputText = DefaultInputText;
    private string _outputText = "Press F5 to generate from the example input.";
    private string _statusText = "Ready";
    private string? _currentFilePath;
    private bool _isRunning;
    private bool _scrollOutputToTop;

    public MainWindowViewModel(Window window)
    {
        _window = window;
        _workspaceManager = new WorkspaceManager();
        _toolLocator = new ToolLocator();
        _processRunner = new ProcessRunner(AppendOutput);
        _figureConverter = new FigureConverter(_processRunner, AppendOutput);
        _latestWorkspace = _workspaceManager.FindLatestRun();

        NewCommand = new AsyncRelayCommand(NewFileAsync, () => !IsRunning);
        OpenCommand = new AsyncRelayCommand(OpenFileAsync, () => !IsRunning);
        SaveCommand = new AsyncRelayCommand(SaveFileAsync);
        SaveAsCommand = new AsyncRelayCommand(SaveAsFileAsync);
        GenerateCommand = new AsyncRelayCommand(GenerateAsync, () => !IsRunning && !string.IsNullOrWhiteSpace(InputText));
        StopCommand = new RelayCommand(_ => StopExecution(), _ => IsRunning);
        FiguresCommand = new AsyncRelayCommand(GenerateFiguresAsync, () => !IsRunning);
        ClearOutputCommand = new RelayCommand(_ => ClearOutput());
        OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());
        ExitCommand = new RelayCommand(_ => _window.Close());
        AboutCommand = new RelayCommand(_ => ShowAbout());
        QuickStartCommand = new RelayCommand(_ => ShowQuickStart());
        ReferenceCommand = new RelayCommand(_ => ShowReference());
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            var wasDirty = IsDirty;
            if (!SetProperty(ref _inputText, value))
                return;

            OnPropertyChanged(nameof(InputStatistics));
            (GenerateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

            if (wasDirty != IsDirty)
            {
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set => SetProperty(ref _outputText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (!SetProperty(ref _currentFilePath, value))
                return;

            OnPropertyChanged(nameof(CurrentFileName));
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value))
                return;

            OnPropertyChanged(nameof(IsIdle));
            RaiseCommandStates();
        }
    }

    public bool ScrollOutputToTop
    {
        get => _scrollOutputToTop;
        set => SetProperty(ref _scrollOutputToTop, value);
    }

    public bool IsIdle => !IsRunning;
    public bool IsDirty => !string.Equals(InputText, _savedInputText, StringComparison.Ordinal);
    public string CurrentFileName => CurrentFilePath is null ? "Untitled" : Path.GetFileName(CurrentFilePath);
    public string WindowTitle => $"{(IsDirty ? "● " : string.Empty)}{CurrentFileName} — {AppInfo.Name}";
    public string InputStatistics => $"{CountLines(InputText)} lines  •  {InputText.Length:N0} characters";
    public string WorkspaceText => _latestWorkspace is null
        ? "No runs yet"
        : $"Latest run: {Path.GetFileName(_latestWorkspace.RootDirectory)}";

    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand GenerateCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand FiguresCommand { get; }
    public ICommand ClearOutputCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand QuickStartCommand { get; }
    public ICommand ReferenceCommand { get; }

    public async Task<bool> ConfirmCloseAsync()
    {
        if (!IsDirty)
            return true;

        var dialog = new UnsavedChangesDialog(CurrentFileName);
        var choice = await dialog.ShowDialog<UnsavedChangesChoice>(_window);

        switch (choice)
        {
            case UnsavedChangesChoice.Discard:
                return true;

            case UnsavedChangesChoice.Save:
                await SaveFileAsync();
                return !IsDirty;

            default:
                return false;
        }
    }

    public void Shutdown() => Dispose();

    public void Dispose()
    {
        _operationCancellation?.Cancel();
        _processRunner.CancelCurrent();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
        GC.SuppressFinalize(this);
    }

    private async Task NewFileAsync()
    {
        if (!await ConfirmCloseAsync())
            return;

        InputText = DefaultInputText;
        CurrentFilePath = null;
        MarkCurrentInputSaved();
        StatusText = "New configuration";
    }

    private async Task OpenFileAsync()
    {
        if (!await ConfirmCloseAsync())
            return;

        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open input configuration",
            AllowMultiple = false,
            FileTypeFilter = ConfigurationFileTypes
        });

        if (files.Count == 0)
            return;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            InputText = await reader.ReadToEndAsync();
            CurrentFilePath = files[0].TryGetLocalPath();
            MarkCurrentInputSaved();
            StatusText = $"Opened {CurrentFileName}";
        }
        catch (Exception exception)
        {
            ReportError("Could not open the configuration", exception);
        }
    }

    private async Task SaveFileAsync()
    {
        if (CurrentFilePath is null)
        {
            await SaveAsFileAsync();
            return;
        }

        try
        {
            await File.WriteAllTextAsync(CurrentFilePath, InputText);
            MarkCurrentInputSaved();
            StatusText = $"Saved {CurrentFileName}";
        }
        catch (Exception exception)
        {
            ReportError("Could not save the configuration", exception);
        }
    }

    private async Task SaveAsFileAsync()
    {
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save input configuration",
            DefaultExtension = "txt",
            SuggestedFileName = CurrentFilePath is null ? "input.txt" : Path.GetFileName(CurrentFilePath),
            FileTypeChoices = ConfigurationFileTypes
        });

        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(InputText);
            await writer.FlushAsync();

            CurrentFilePath = file.TryGetLocalPath();
            MarkCurrentInputSaved();
            StatusText = $"Saved {CurrentFileName}";
        }
        catch (Exception exception)
        {
            ReportError("Could not save the configuration", exception);
        }
    }

    private async Task GenerateAsync()
    {
        var validationErrors = InputValidator.Validate(InputText);
        if (validationErrors.Count > 0)
        {
            SetConsoleContent(
                "Invalid input:\n\n" +
                string.Join(Environment.NewLine, validationErrors.Select(error => $"  • {error}")) +
                "\n\nSee Input Reference.");
            StatusText = "Invalid input";
            return;
        }

        BeginOperation("Preparing generation", clearOutput: true);
        var cancellationToken = _operationCancellation!.Token;

        try
        {
            var engine = _toolLocator.FindEngine()
                ?? throw new FileNotFoundException(
                    "GeoGen was not found. Reinstall the application or run the packaging script.");

            var defaultSettings = Path.Combine(engine.WorkingDirectory, "settings.json");
            if (!File.Exists(defaultSettings))
                throw new FileNotFoundException("The engine settings file is missing.", defaultSettings);

            var workspace = _workspaceManager.CreateRunWorkspace();
            _latestWorkspace = workspace;
            OnPropertyChanged(nameof(WorkspaceText));

            await _workspaceManager.PrepareEngineRunAsync(workspace, InputText, cancellationToken);

            AppendOutput($"{AppInfo.Name} {AppInfo.Version}{Environment.NewLine}");
            AppendOutput($"Run folder: {workspace.RootDirectory}{Environment.NewLine}{Environment.NewLine}");
            StatusText = "Generating theorems";

            var arguments = engine.PrefixArguments
                .Concat(new[] { defaultSettings, workspace.SettingsFilePath })
                .ToArray();

            var result = await _processRunner.RunAsync(
                engine.ExecutablePath,
                arguments,
                engine.WorkingDirectory,
                standardInputLines: null,
                cancellationToken);

            if (result.ExitCode != 0)
                throw new InvalidOperationException($"The generator exited with code {result.ExitCode}.");

            var jsonCount = CountFiles(workspace.JsonOutputDirectory, "*.json");
            var readableCount = CountFiles(workspace.ReadableOutputDirectory, "*.txt");

            AppendOutput(Environment.NewLine + new string('─', 64) + Environment.NewLine);
            AppendOutput($"Done: {readableCount} text file(s), {jsonCount} JSON file(s){Environment.NewLine}");
            AppendOutput($"Results: {workspace.OutputDirectory}{Environment.NewLine}");

            if (result.Contains("Interesting theorems: 0"))
            {
                AppendOutput(
                    Environment.NewLine +
                    "No non-trivial theorems were found. Increase an object limit, add an initial object, " +
                    "or change the construction list." +
                    Environment.NewLine);
            }

            StatusText = "Generation complete";
        }
        catch (OperationCanceledException)
        {
            AppendOutput(Environment.NewLine + "Generation cancelled." + Environment.NewLine);
            StatusText = "Generation cancelled";
        }
        catch (Exception exception)
        {
            ReportError("Generation failed", exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task GenerateFiguresAsync()
    {
        var workspace = _latestWorkspace ?? _workspaceManager.FindLatestRun();
        if (workspace is null)
        {
            SetConsoleContent("No run found. Generate theorems first.");
            StatusText = "No generated run";
            return;
        }

        var jsonFile = FindLatestFile(workspace.JsonOutputDirectory, "*.json");
        if (jsonFile is null)
        {
            SetConsoleContent("The latest run has no theorem JSON.");
            StatusText = "No theorem JSON found";
            return;
        }

        var destinationFolders = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose where to save the figures",
            AllowMultiple = false
        });

        if (destinationFolders.Count == 0)
            return;

        var destinationDirectory = destinationFolders[0].TryGetLocalPath();
        if (destinationDirectory is null)
        {
            SetConsoleContent("The selected destination is not a local folder.");
            StatusText = "Unsupported destination";
            return;
        }

        BeginOperation("Preparing figures", clearOutput: false);
        var cancellationToken = _operationCancellation!.Token;

        try
        {
            var theoremCount = await CountJsonArrayItemsAsync(jsonFile, cancellationToken);
            if (theoremCount == 0)
                throw new InvalidOperationException("The selected JSON file contains no theorems to draw.");

            var drawer = _toolLocator.FindDrawer()
                ?? throw new FileNotFoundException(
                    "The drawing tool was not found. Reinstall the application or run the packaging script.");

            var figureWorkspace = await _workspaceManager.PrepareFigureWorkspaceAsync(
                workspace,
                drawer.WorkingDirectory,
                cancellationToken);

            AppendOutput(Environment.NewLine + new string('─', 64) + Environment.NewLine);
            AppendOutput($"Drawing {theoremCount} theorem(s) from {Path.GetFileName(jsonFile)}{Environment.NewLine}");
            StatusText = "Rendering MetaPost figures";

            var result = await _processRunner.RunAsync(
                drawer.ExecutablePath,
                drawer.PrefixArguments,
                figureWorkspace,
                new[] { jsonFile, $"1-{theoremCount}" },
                cancellationToken);

            if (result.ExitCode != 0)
                throw new InvalidOperationException($"The drawing tool exited with code {result.ExitCode}.");

            var failedFigures = result.AllLines
                .Select(line => FailedFigureRegex().Match(line))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
                .Distinct()
                .Order()
                .ToArray();

            StatusText = "Converting figures";
            var figureDataDirectory = Path.Combine(figureWorkspace, "Data");
            var conversion = await _figureConverter.ConvertAsync(
                figureDataDirectory,
                destinationDirectory,
                cancellationToken);

            if (conversion.SourceCount == 0)
            {
                AppendMetaPostDiagnostics(figureDataDirectory);
                throw new InvalidOperationException(
                    "MetaPost produced no figures. Check that TeX Live or MiKTeX is installed and 'mpost' is on PATH.");
            }

            AppendOutput(Environment.NewLine + new string('─', 64) + Environment.NewLine);
            AppendOutput($"Figures requested: {theoremCount}{Environment.NewLine}");
            AppendOutput($"Figures rendered: {conversion.SourceCount}{Environment.NewLine}");
            AppendOutput($"PDF files: {conversion.ConvertedCount}{Environment.NewLine}");
            if (conversion.EpsFallbackCount > 0)
                AppendOutput($"EPS fallbacks: {conversion.EpsFallbackCount}{Environment.NewLine}");
            if (failedFigures.Length > 0)
                AppendOutput($"Construction failures: {string.Join(", ", failedFigures)}{Environment.NewLine}");
            AppendOutput($"Saved in: {destinationDirectory}{Environment.NewLine}");

            StatusText = failedFigures.Length == 0
                ? "Figures complete"
                : $"Figures complete with {failedFigures.Length} construction failure(s)";
        }
        catch (OperationCanceledException)
        {
            AppendOutput(Environment.NewLine + "Figure generation cancelled." + Environment.NewLine);
            StatusText = "Figure generation cancelled";
        }
        catch (Exception exception)
        {
            ReportError("Figure generation failed", exception);
        }
        finally
        {
            EndOperation();
        }
    }

    private void StopExecution()
    {
        StatusText = "Stopping";
        _operationCancellation?.Cancel();
        _processRunner.CancelCurrent();
    }

    private void OpenOutputFolder()
    {
        var workspace = _latestWorkspace ?? _workspaceManager.FindLatestRun();
        var directory = workspace?.OutputDirectory ?? _workspaceManager.RootDirectory;

        if (!Directory.Exists(directory))
        {
            AppendOutput("No results found. Generate a run first." + Environment.NewLine);
            StatusText = "No results folder";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            ReportError("Could not open the results folder", exception);
        }
    }

    private void ShowQuickStart()
    {
        SetConsoleContent(HelpContent.QuickStart);
        StatusText = "Quick Start";
    }

    private void ShowReference()
    {
        SetConsoleContent(HelpContent.Reference);
        StatusText = "Input reference";
    }

    private void ShowAbout()
    {
        SetConsoleContent(
            $"""
            {AppInfo.Name} {AppInfo.Version}

            A desktop application for generating, proving, ranking, and drawing planar geometry
            theorems. Based on GeoGen by Patrik Bak.

            Studio: {AppInfo.RepositoryUrl}
            GeoGen: {AppInfo.GeoGenRepositoryUrl}
            License: GNU Affero General Public License v3.0
            """);
        StatusText = $"{AppInfo.Name} {AppInfo.Version}";
    }

    private void BeginOperation(string status, bool clearOutput)
    {
        _operationCancellation?.Dispose();
        _operationCancellation = new CancellationTokenSource();
        IsRunning = true;
        StatusText = status;

        if (clearOutput)
            ClearOutput();
    }

    private void EndOperation()
    {
        StopFlushTimer();
        _operationCancellation?.Dispose();
        _operationCancellation = null;
        IsRunning = false;
    }

    private void RaiseCommandStates()
    {
        (NewCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (OpenCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (GenerateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (FiguresCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void MarkCurrentInputSaved()
    {
        _savedInputText = InputText;
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(WindowTitle));
    }

    private void ClearOutput()
    {
        lock (_bufferLock)
            _outputBuffer.Clear();
        OutputText = string.Empty;
    }

    private void SetConsoleContent(string content)
    {
        lock (_bufferLock)
            _outputBuffer.Clear();
        OutputText = content;
        ScrollOutputToTop = true;
    }

    private void AppendOutput(string text)
    {
        lock (_bufferLock)
            _outputBuffer.Append(text);
        EnsureFlushTimerRunning();
    }

    private void EnsureFlushTimerRunning()
    {
        if (_flushTimer is not null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_flushTimer is not null)
                return;

            _flushTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(FlushIntervalMilliseconds)
            };
            _flushTimer.Tick += (_, _) => FlushOutputBuffer();
            _flushTimer.Start();
        });
    }

    private void StopFlushTimer()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _flushTimer?.Stop();
            _flushTimer = null;
            FlushOutputBuffer();
        });
    }

    private void FlushOutputBuffer()
    {
        string chunk;
        lock (_bufferLock)
        {
            if (_outputBuffer.Length == 0)
                return;

            chunk = _outputBuffer.ToString();
            _outputBuffer.Clear();
        }

        var current = OutputText + chunk;
        if (current.Length > MaximumOutputLength)
        {
            var trimStart = current.Length - MaximumOutputLength;
            var nextLine = current.IndexOf('\n', trimStart);
            current = "[… earlier output trimmed …]" + Environment.NewLine +
                      current[(nextLine >= 0 ? nextLine + 1 : trimStart)..];
        }

        OutputText = current;
    }

    private void ReportError(string message, Exception exception)
    {
        StatusText = message;
        AppendOutput($"{Environment.NewLine}[ERROR] {message}: {exception.Message}{Environment.NewLine}");
    }

    private static int CountLines(string text) =>
        string.IsNullOrEmpty(text) ? 0 : text.Count(character => character == '\n') + 1;

    private static int CountFiles(string directory, string pattern) =>
        Directory.Exists(directory) ? Directory.EnumerateFiles(directory, pattern).Count() : 0;

    private static string? FindLatestFile(string directory, string pattern) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, pattern).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;

    private static async Task<int> CountJsonArrayItemsAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.GetArrayLength()
            : 1;
    }

    private void AppendMetaPostDiagnostics(string figureDataDirectory)
    {
        var logPath = Path.Combine(figureDataDirectory, "figures.log");
        if (!File.Exists(logPath))
            return;

        try
        {
            var tail = File.ReadLines(logPath).TakeLast(30);
            AppendOutput(Environment.NewLine + "MetaPost log (last 30 lines):" + Environment.NewLine);
            foreach (var line in tail)
                AppendOutput("  " + line + Environment.NewLine);
        }
        catch
        {
            // Diagnostics should never hide the original rendering error.
        }
    }

    [GeneratedRegex(@"Picture number (\d+) couldn't be constructed", RegexOptions.IgnoreCase)]
    private static partial Regex FailedFigureRegex();
}
