using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GeoGen.DesktopApp.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace GeoGen.DesktopApp.ViewModels;

/// <summary>
/// Main ViewModel for Planar Geometry Studio. Manages the code editor, console output,
/// problem generation, and figure generation workflows.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    #region Constants

    private const string DefaultInputText =
        """
        Constructions:

         Centroid
         CircleWithCenterThroughPoint
         CircleWithDiameter
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

    #endregion

    #region Private fields

    private readonly Window _window;
    private Process? _currentProcess;
    private CancellationTokenSource? _cts;

    // Buffered output to avoid flooding the UI thread with per-line updates
    private readonly StringBuilder _outputBuffer = new();
    private readonly object _bufferLock = new();
    private DispatcherTimer? _flushTimer;
    private const int FlushIntervalMs = 150; // flush output every 150ms
    private const int MaxOutputLength = 500_000; // trim output if it exceeds this

    #endregion

    #region Observable properties

    private string _inputText = DefaultInputText;
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    private string _outputText = "";
    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string? _currentFilePath;
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (SetProperty(ref _currentFilePath, value))
                OnPropertyChanged(nameof(CurrentFileName));
        }
    }

    public string CurrentFileName => _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "Untitled";

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                (GenerateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (FiguresCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string WindowTitle => _currentFilePath != null
        ? $"Planar Geometry Studio - {Path.GetFileName(_currentFilePath)}"
        : "Planar Geometry Studio";

    private bool _scrollOutputToTop;
    public bool ScrollOutputToTop
    {
        get => _scrollOutputToTop;
        set => SetProperty(ref _scrollOutputToTop, value);
    }

    #endregion

    #region Commands

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

    #endregion

    #region Constructor

    public MainWindowViewModel(Window window)
    {
        _window = window;

        NewCommand = new RelayCommand(_ => NewFile());
        OpenCommand = new AsyncRelayCommand(OpenFileAsync);
        SaveCommand = new AsyncRelayCommand(SaveFileAsync);
        SaveAsCommand = new AsyncRelayCommand(SaveAsFileAsync);
        GenerateCommand = new AsyncRelayCommand(GenerateAsync, () => !IsRunning);
        StopCommand = new RelayCommand(_ => StopExecution(), _ => IsRunning);
        FiguresCommand = new AsyncRelayCommand(GenerateFiguresAsync, () => !IsRunning);
        ClearOutputCommand = new RelayCommand(_ => OutputText = "");
        OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());
        ExitCommand = new RelayCommand(_ => _window.Close());
        AboutCommand = new RelayCommand(_ => ShowAbout());
        QuickStartCommand = new RelayCommand(_ => ShowQuickStart());
        ReferenceCommand = new RelayCommand(_ => ShowReference());
    }

    #endregion

    #region File operations

    private void NewFile()
    {
        InputText = DefaultInputText;
        CurrentFilePath = null;
        OnPropertyChanged(nameof(WindowTitle));
        StatusText = "New file created";
    }

    private async Task OpenFileAsync()
    {
        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Input File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count == 0) return;

        var file = files[0];
        try
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            InputText = await reader.ReadToEndAsync();
            CurrentFilePath = file.TryGetLocalPath();
            OnPropertyChanged(nameof(WindowTitle));
            StatusText = $"Opened: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            AppendOutput($"[ERROR] Failed to open file: {ex.Message}\n");
        }
    }

    private async Task SaveFileAsync()
    {
        if (CurrentFilePath != null)
        {
            try
            {
                await File.WriteAllTextAsync(CurrentFilePath, InputText);
                StatusText = $"Saved: {CurrentFileName}";
            }
            catch (Exception ex)
            {
                AppendOutput($"[ERROR] Failed to save: {ex.Message}\n");
            }
        }
        else
        {
            await SaveAsFileAsync();
        }
    }

    private async Task SaveAsFileAsync()
    {
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Input File",
            DefaultExtension = "txt",
            SuggestedFileName = CurrentFilePath != null ? Path.GetFileName(CurrentFilePath) : "input.txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (file == null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(InputText);
            CurrentFilePath = file.TryGetLocalPath();
            OnPropertyChanged(nameof(WindowTitle));
            StatusText = $"Saved: {CurrentFileName}";
        }
        catch (Exception ex)
        {
            AppendOutput($"[ERROR] Failed to save: {ex.Message}\n");
        }
    }

    #endregion

    #region Problem generation

    private async Task GenerateAsync()
    {
        IsRunning = true;
        lock (_bufferLock) { _outputBuffer.Clear(); }
        OutputText = "";
        StatusText = "Generating problems...";
        _cts = new CancellationTokenSource();

        try
        {
            var engineDir = FindEngineDirectory();
            if (engineDir == null)
            {
                AppendOutput("[ERROR] Could not find GeoGen engine.\n");
                AppendOutput("Please ensure the engine is published to the 'tools/engine' directory.\n");
                AppendOutput("Run the publish script: ./publish.sh (Linux) or .\\publish.ps1 (Windows)\n");
                StatusText = "Engine not found";
                return;
            }

            AppendOutput($"Engine directory: {engineDir}\n");

            // Write the current input to the engine's input directory
            var inputDir = Path.Combine(engineDir, "Examples", "Inputs");
            Directory.CreateDirectory(inputDir);

            // Clear old input files
            foreach (var f in Directory.GetFiles(inputDir, "input*.txt"))
                File.Delete(f);

            // Write the current input
            var inputPath = Path.Combine(inputDir, "input.txt");
            await File.WriteAllTextAsync(inputPath, InputText);
            AppendOutput($"Input saved to: {inputPath}\n");

            // Create output directories
            var outputDir = Path.Combine(engineDir, "Examples", "Output");
            Directory.CreateDirectory(Path.Combine(outputDir, "ReadableWithoutProofs"));
            Directory.CreateDirectory(Path.Combine(outputDir, "ReadableWithProofs"));
            Directory.CreateDirectory(Path.Combine(outputDir, "JsonOutput"));
            Directory.CreateDirectory(Path.Combine(outputDir, "ReadableBestTheorems"));
            Directory.CreateDirectory(Path.Combine(outputDir, "JsonBestTheorems"));

            // Find GeoGen executable
            var exeName = GetExecutableName("GeoGen");
            var exePath = Path.Combine(engineDir, exeName);

            if (!File.Exists(exePath))
            {
                // Try with .dll for cross-platform
                exePath = Path.Combine(engineDir, "GeoGen.dll");
                if (!File.Exists(exePath))
                {
                    AppendOutput($"[ERROR] GeoGen executable not found at: {exePath}\n");
                    StatusText = "Executable not found";
                    return;
                }
                // Run with dotnet
                AppendOutput("Starting generation with dotnet...\n\n");
                await RunProcessAsync("dotnet", engineDir, exePath);
            }
            else
            {
                AppendOutput("Starting generation...\n\n");
                await RunProcessAsync(exePath, engineDir);
            }

            StatusText = _cts.IsCancellationRequested ? "Generation stopped" : "Generation complete!";
            AppendOutput($"\n{'='} Generation {(_cts.IsCancellationRequested ? "stopped" : "complete")} {'='}\n");

            // Show output location
            var jsonOutputDir = Path.Combine(outputDir, "JsonOutput");
            if (Directory.Exists(jsonOutputDir))
            {
                var jsonFiles = Directory.GetFiles(jsonOutputDir, "*.json");
                if (jsonFiles.Length > 0)
                    AppendOutput($"\nJSON output files ({jsonFiles.Length}) in: {jsonOutputDir}\n");
            }

            var readableDir = Path.Combine(outputDir, "ReadableWithoutProofs");
            if (Directory.Exists(readableDir))
            {
                var readableFiles = Directory.GetFiles(readableDir, "*.txt");
                if (readableFiles.Length > 0)
                    AppendOutput($"Readable output files ({readableFiles.Length}) in: {readableDir}\n");
            }

            // Check for 0 interesting theorems and show guidance
            if (!_cts.IsCancellationRequested && OutputText.Contains("Interesting theorems: 0"))
            {
                AppendOutput("\n--- Tip ---");
                AppendOutput("\nThe engine found 0 interesting theorems. This is normal for certain");
                AppendOutput("\ninput configurations. The theorem prover is very powerful and can");
                AppendOutput("\noften prove all discovered theorems trivially.\n");
                AppendOutput("\nTo get results, try:");
                AppendOutput("\n  - Increase MaximalPoints/Lines/Circles (try 2-3). With many");
                AppendOutput("\n    starting objects, the prover trivially proves results from");
                AppendOutput("\n    just 1 new object; adding more creates non-trivial interactions.");
                AppendOutput("\n  - Use a richer initial configuration with constructions like");
                AppendOutput("\n    Incenter, Circumcenter, Orthocenter, or Midpoint");
                AppendOutput("\n  - Add perpendicular projections of special points onto sides");
                AppendOutput("\n  - Keep iterations low (1-2) with targeted maximal objects");
                AppendOutput("\n  - Use the default template (File > New) as a starting point\n");
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Generation cancelled";
            AppendOutput("\n=== Generation Cancelled ===\n");
        }
        catch (Exception ex)
        {
            StatusText = "Generation failed";
            AppendOutput($"\n[ERROR] {ex.Message}\n");
            if (ex.InnerException != null)
                AppendOutput($"  Inner: {ex.InnerException.Message}\n");
        }
        finally
        {
            StopFlushTimer();
            IsRunning = false;
            _cts = null;
        }
    }

    #endregion

    #region Figure generation

    private async Task GenerateFiguresAsync()
    {
        IsRunning = true;
        StatusText = "Preparing figure generation...";

        try
        {
            // Step 1: Auto-detect the JSON output file
            var engineDir = FindEngineDirectory();
            if (engineDir == null)
            {
                AppendOutput("[ERROR] Could not find GeoGen engine.\n");
                StatusText = "Engine not found";
                IsRunning = false;
                return;
            }

            var jsonOutputDir = Path.Combine(engineDir, "Examples", "Output", "JsonOutput");
            if (!Directory.Exists(jsonOutputDir))
            {
                AppendOutput("[ERROR] JSON output directory not found. Run generation first.\n");
                AppendOutput($"Expected at: {jsonOutputDir}\n");
                StatusText = "No JSON output found";
                IsRunning = false;
                return;
            }

            var jsonFiles = Directory.GetFiles(jsonOutputDir, "*.json")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();

            if (jsonFiles.Length == 0)
            {
                AppendOutput("[ERROR] No JSON files found in the output directory. Run generation first.\n");
                AppendOutput($"Searched in: {jsonOutputDir}\n");
                StatusText = "No JSON files found";
                IsRunning = false;
                return;
            }

            var jsonFilePath = jsonFiles[0];
            AppendOutput($"Using JSON file: {jsonFilePath}\n");
            if (jsonFiles.Length > 1)
                AppendOutput($"  (selected most recent of {jsonFiles.Length} JSON files)\n");

            // Step 2: Let user choose where to save the PDF figures
            var outputFolder = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose Folder to Save PDF Figures",
                AllowMultiple = false
            });

            if (outputFolder.Count == 0)
            {
                StatusText = "Figure generation cancelled";
                IsRunning = false;
                return;
            }

            var outputFolderPath = outputFolder[0].TryGetLocalPath();
            if (outputFolderPath == null)
            {
                AppendOutput("[ERROR] Could not get local folder path.\n");
                StatusText = "Error";
                IsRunning = false;
                return;
            }

            AppendOutput($"Output folder: {outputFolderPath}\n");

            // Count theorems in the JSON file
            int theoremCount;
            try
            {
                var jsonText = await File.ReadAllTextAsync(jsonFilePath);
                using var doc = JsonDocument.Parse(jsonText);
                theoremCount = doc.RootElement.GetArrayLength();
                AppendOutput($"Found {theoremCount} theorems in the file.\n");
            }
            catch
            {
                // Fallback: try counting JSON objects
                var lines = await File.ReadAllLinesAsync(jsonFilePath);
                theoremCount = lines.Count(l => l.TrimStart().StartsWith("{"));
                if (theoremCount == 0) theoremCount = 1;
                AppendOutput($"Estimated {theoremCount} theorems.\n");
            }

            if (theoremCount == 0)
            {
                AppendOutput("[INFO] The JSON file contains 0 theorems — nothing to draw.\n");
                AppendOutput("Try running generation with different parameters (e.g. increase MaximalPoints/Lines/Circles).\n");
                StatusText = "No theorems to draw";
                IsRunning = false;
                return;
            }

            // Step 3: Run the DrawingLauncher
            var drawerDir = FindDrawerDirectory();
            if (drawerDir == null)
            {
                AppendOutput("[ERROR] Could not find DrawingLauncher.\n");
                AppendOutput("Please ensure it is published to the 'tools/drawer' directory.\n");
                StatusText = "Drawer not found";
                IsRunning = false;
                return;
            }

            var drawerExe = GetExecutableName("GeoGen.DrawingLauncher");
            var drawerExePath = Path.Combine(drawerDir, drawerExe);
            var useDotnet = false;

            if (!File.Exists(drawerExePath))
            {
                drawerExePath = Path.Combine(drawerDir, "GeoGen.DrawingLauncher.dll");
                if (!File.Exists(drawerExePath))
                {
                    AppendOutput($"[ERROR] DrawingLauncher executable not found.\n");
                    StatusText = "Executable not found";
                    IsRunning = false;
                    return;
                }
                useDotnet = true;
            }

            AppendOutput($"\nGenerating figures (1-{theoremCount})...\n");
            StatusText = "Generating MetaPost figures...";

            // Capture output to detect failed figures
            var capturedOutput = new List<string>();
            var stdinLines = new[] { jsonFilePath, $"1-{theoremCount}" };

            if (useDotnet)
                await RunInteractiveProcessAsync("dotnet", drawerDir, drawerExePath, stdinLines, capturedOutput);
            else
                await RunInteractiveProcessAsync(drawerExePath, drawerDir, null, stdinLines, capturedOutput);

            // Step 4: Parse failed figures from the captured output
            var failedFigures = new List<int>();
            var failedReasons = new Dictionary<int, string>();
            var failedPattern = new Regex(@"Picture number (\d+) couldn't be constructed", RegexOptions.IgnoreCase);

            foreach (var line in capturedOutput)
            {
                var match = failedPattern.Match(line);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var figNum))
                {
                    if (!failedFigures.Contains(figNum))
                        failedFigures.Add(figNum);
                }
            }

            // Also look for reason lines (the line after "couldn't be constructed" usually has the exception type)
            for (int i = 0; i < capturedOutput.Count; i++)
            {
                var match = failedPattern.Match(capturedOutput[i]);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var figNum))
                {
                    // Look for the exception message in the next lines
                    for (int j = i + 1; j < Math.Min(i + 5, capturedOutput.Count); j++)
                    {
                        var reasonLine = capturedOutput[j];
                        if (reasonLine.Contains("Exception:") || reasonLine.Contains("The message:"))
                        {
                            if (!failedReasons.ContainsKey(figNum))
                            {
                                // Extract just the meaningful part
                                var reason = reasonLine.Trim();
                                if (reason.Contains(":"))
                                    reason = reason[(reason.IndexOf(':') + 1)..].Trim();
                                failedReasons[figNum] = reason;
                            }
                            break;
                        }
                    }
                }
            }

            failedFigures.Sort();

            // Step 5: Convert EPS files to PDF in the user's chosen folder
            AppendOutput("\n--- Converting figures to PDF ---\n");
            StatusText = "Converting EPS to PDF...";

            var dataDir = Path.Combine(drawerDir, "Data");
            if (!Directory.Exists(dataDir))
                dataDir = drawerDir;

            var (convertedCount, totalEps) = await ConvertEpsToPdfAsync(dataDir, outputFolderPath);

            // Step 6: Summary
            AppendOutput("\n" + new string('=', 50) + "\n");
            AppendOutput("  FIGURE GENERATION SUMMARY\n");
            AppendOutput(new string('=', 50) + "\n\n");
            AppendOutput($"  Total theorems:       {theoremCount}\n");
            AppendOutput($"  Figures generated:    {theoremCount - failedFigures.Count}\n");
            AppendOutput($"  Figures failed:       {failedFigures.Count}\n");
            AppendOutput($"  PDFs converted:       {convertedCount}/{totalEps}\n");
            AppendOutput($"  Output folder:        {outputFolderPath}\n");

            if (failedFigures.Count > 0)
            {
                AppendOutput($"\n  Failed figures: {string.Join(", ", failedFigures)}\n");
                foreach (var fig in failedFigures)
                {
                    if (failedReasons.TryGetValue(fig, out var reason))
                        AppendOutput($"    Figure {fig}: {reason}\n");
                    else
                        AppendOutput($"    Figure {fig}: (no details available)\n");
                }
            }

            AppendOutput("\n" + new string('=', 50) + "\n");

            StatusText = failedFigures.Count > 0
                ? $"Done! {convertedCount} PDFs saved, {failedFigures.Count} failed"
                : $"Done! {convertedCount} PDFs saved to output folder";
        }
        catch (Exception ex)
        {
            StatusText = "Figure generation failed";
            AppendOutput($"\n[ERROR] {ex.Message}\n");
        }
        finally
        {
            StopFlushTimer();
            IsRunning = false;
        }
    }

    private async Task<(int convertedCount, int totalEps)> ConvertEpsToPdfAsync(string epsDirectory, string pdfOutputDirectory)
    {
        Directory.CreateDirectory(pdfOutputDirectory);

        // Search for figure files in multiple possible locations
        var searchDirs = new List<string> { epsDirectory };

        // Also check the drawer directory itself (parent of Data/)
        var parentDir = Path.GetDirectoryName(epsDirectory);
        if (parentDir != null && parentDir != epsDirectory)
            searchDirs.Add(parentDir);

        var epsFiles = Array.Empty<string>();

        foreach (var searchDir in searchDirs)
        {
            if (!Directory.Exists(searchDir))
                continue;

            var allFiles = Directory.GetFiles(searchDir);

            // Look for figures.N pattern (MetaPost output) — also match figures.N.ext variants
            epsFiles = allFiles
                .Where(f =>
                {
                    var name = Path.GetFileName(f);
                    return Regex.IsMatch(name, @"^figures\.\d+(\.\w+)?$");
                })
                // Exclude known non-figure files
                .Where(f => !f.EndsWith(".mp") && !f.EndsWith(".log") && !f.EndsWith(".mpx"))
                .OrderBy(f =>
                {
                    var match = Regex.Match(Path.GetFileName(f), @"\.(\d+)");
                    return match.Success ? int.Parse(match.Groups[1].Value) : 0;
                })
                .ToArray();

            if (epsFiles.Length > 0)
            {
                AppendOutput($"Found {epsFiles.Length} figure file(s) in: {searchDir}\n");
                break;
            }
        }

        if (epsFiles.Length == 0)
        {
            // Show the MetaPost compilation log for diagnostics
            var logFile = Path.Combine(epsDirectory, "figures.log");
            if (File.Exists(logFile))
            {
                try
                {
                    var logLines = await File.ReadAllLinesAsync(logFile);
                    AppendOutput("MetaPost compilation log (figures.log):\n");
                    // Show the full log — it contains the error that prevented figure generation
                    foreach (var line in logLines)
                        AppendOutput($"  {line}\n");
                    AppendOutput("\n");
                }
                catch { }
            }

            AppendOutput("No figure files (figures.1, figures.2, ...) found.\n");
            AppendOutput("This usually means MetaPost (mpost) failed to compile the figures.\n");
            AppendOutput("Check that a TeX distribution (TeX Live or MiKTeX) is installed and 'mpost' is on your PATH.\n\n");

            foreach (var searchDir in searchDirs)
            {
                if (Directory.Exists(searchDir))
                {
                    var files = Directory.GetFiles(searchDir).Select(Path.GetFileName).ToArray();
                    AppendOutput($"  Files in {searchDir}: {(files.Length > 0 ? string.Join(", ", files.Take(30)) : "(empty)")}\n");
                    if (files.Length > 30)
                        AppendOutput($"    ... and {files.Length - 30} more\n");
                }
                else
                {
                    AppendOutput($"  Directory not found: {searchDir}\n");
                }
            }
            return (0, 0);
        }

        AppendOutput($"  PDF output:  {pdfOutputDirectory}\n");
        var convertedCount = 0;

        // Determine the actual directory where EPS files reside
        var actualEpsDir = Path.GetDirectoryName(epsFiles[0]) ?? epsDirectory;

        foreach (var epsFile in epsFiles)
        {
            var fileName = Path.GetFileName(epsFile);
            var pdfFile = Path.Combine(pdfOutputDirectory, fileName + ".pdf");
            AppendOutput($"  Converting {fileName} -> {fileName}.pdf ... ");

            var success = false;

            // Try epstopdf (TeX Live / MiKTeX)
            success = await TryRunConversionAsync("epstopdf",
                $"--outfile=\"{pdfFile}\" \"{epsFile}\"", epsDirectory);

            // Try miktex-epstopdf
            if (!success)
            {
                success = await TryRunConversionAsync("miktex-epstopdf",
                    $"--outfile=\"{pdfFile}\" \"{epsFile}\"", epsDirectory);
            }

            // Try Ghostscript (Windows)
            if (!success)
            {
                var gsExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "gswin64c" : "gs";
                success = await TryRunConversionAsync(gsExe,
                    $"-sDEVICE=pdfwrite -dNOPAUSE -dBATCH -dEPSCrop -sOutputFile=\"{pdfFile}\" \"{epsFile}\"",
                    epsDirectory);
            }

            // Try gswin32c on Windows
            if (!success && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                success = await TryRunConversionAsync("gswin32c",
                    $"-sDEVICE=pdfwrite -dNOPAUSE -dBATCH -dEPSCrop -sOutputFile=\"{pdfFile}\" \"{epsFile}\"",
                    epsDirectory);
            }

            if (success)
            {
                AppendOutput("OK\n");
                convertedCount++;
            }
            else
            {
                AppendOutput("FAILED (epstopdf/gs not found)\n");
            }
        }

        AppendOutput($"\nConverted {convertedCount}/{epsFiles.Length} figures to PDF.\n");

        if (convertedCount < epsFiles.Length)
        {
            AppendOutput("\nNote: To convert all figures, install one of:\n");
            AppendOutput("  - TeX Live (provides epstopdf): https://tug.org/texlive/\n");
            AppendOutput("  - MiKTeX (provides epstopdf): https://miktex.org/\n");
            AppendOutput("  - Ghostscript (provides gs/gswin64c): https://ghostscript.com/\n");
        }

        return (convertedCount, epsFiles.Length);
    }

    private async Task<bool> TryRunConversionAsync(string command, string arguments, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Read output asynchronously
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Process management

    /// <summary>
    /// Phrases in stdout/stderr that indicate the GeoGen engine has finished its work.
    /// The engine's Application.Run may call Console.ReadKey() after finishing, which would
    /// cause the process to hang forever when launched as a subprocess. We detect these
    /// messages and kill the process gracefully.
    /// </summary>
    private static readonly string[] FinishedPhrases = new[]
    {
        "The application has finished correctly",
        "Press any key to exit",
        "An unexpected exception has occurred"
    };

    private async Task RunProcessAsync(string exePath, string workingDir, string? arguments = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Set TERM so the engine's ShouldPauseBeforeExit() returns false on Linux
        psi.Environment["TERM"] = "xterm";

        if (arguments != null)
            psi.Arguments = arguments;

        using var process = new Process { StartInfo = psi };
        _currentProcess = process;

        // Track whether the engine signalled completion
        var finishedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                // AppendOutput is thread-safe; no need to post to UI thread
                AppendOutput(e.Data + "\n");

                // Check if the engine has finished its work
                if (FinishedPhrases.Any(phrase => e.Data.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
                {
                    finishedSignal.TrySetResult(true);
                }
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                AppendOutput(e.Data + "\n");

                if (FinishedPhrases.Any(phrase => e.Data.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
                {
                    finishedSignal.TrySetResult(true);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            var cancelToken = _cts?.Token ?? CancellationToken.None;

            // Wait for either: process exit, finish signal, or cancellation
            var processExitTask = process.WaitForExitAsync(cancelToken);
            var completedTask = await Task.WhenAny(processExitTask, finishedSignal.Task);

            if (completedTask == finishedSignal.Task)
            {
                // Engine finished but process may be stuck on Console.ReadKey()
                // Give it a moment to exit naturally, then kill it (async to avoid blocking UI)
                using var delayCts = new CancellationTokenSource();
                var delayTask = Task.Delay(2000, delayCts.Token);
                var exitTask = processExitTask;
                var raceResult = await Task.WhenAny(exitTask, delayTask);
                if (raceResult != exitTask)
                {
                    // Process didn't exit in time — kill it
                    try { process.Kill(entireProcessTree: true); } catch { }
                }
                else
                {
                    delayCts.Cancel(); // clean up the delay
                }
            }
            else
            {
                // Process exited on its own
                await processExitTask;
            }
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        finally
        {
            _currentProcess = null;
        }

        try
        {
            if (!process.HasExited)
                return;
            // Don't report exit code if the engine signalled successful completion
            // (killed processes return -1 which is expected in that case)
            if (process.ExitCode != 0 && !finishedSignal.Task.IsCompletedSuccessfully)
                AppendOutput($"\n[Process exited with code {process.ExitCode}]\n");
        }
        catch { }
    }

    private async Task RunInteractiveProcessAsync(string exePath, string workingDir,
        string? arguments, IEnumerable<string> stdinLines, List<string>? capturedOutput = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        if (arguments != null)
            psi.Arguments = arguments;

        using var process = new Process { StartInfo = psi };
        _currentProcess = process;

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                AppendOutput(e.Data + "\n");
                capturedOutput?.Add(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                AppendOutput(e.Data + "\n");
                capturedOutput?.Add(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Write stdin lines with small delays
        foreach (var line in stdinLines)
        {
            await Task.Delay(200);
            try
            {
                await process.StandardInput.WriteLineAsync(line);
            }
            catch (Exception)
            {
                break; // Process may have exited
            }
        }

        try { process.StandardInput.Close(); } catch { }

        // Wait with timeout
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var linkedCts = _cts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token)
            : timeoutCts;

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
        }
        finally
        {
            _currentProcess = null;
        }
    }

    private void StopExecution()
    {
        _cts?.Cancel();
        try { _currentProcess?.Kill(entireProcessTree: true); } catch { }
        StatusText = "Stopping...";
    }

    #endregion

    #region Path resolution

    private string? FindEngineDirectory()
    {
        var baseDir = AppContext.BaseDirectory;

        // Check published layout: tools/engine/
        var publishedPath = Path.Combine(baseDir, "tools", "engine");
        if (IsValidEngineDir(publishedPath))
            return publishedPath;

        // Check sibling layout (both tools in same dir)
        if (IsValidEngineDir(baseDir))
            return baseDir;

        // Development layout: navigate up from bin/Debug/net10.0/
        var devPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..",
            "GeoGen.MainLauncher", "bin", "Debug", "net10.0"));
        if (IsValidEngineDir(devPath))
            return devPath;

        // Try Release
        devPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..",
            "GeoGen.MainLauncher", "bin", "Release", "net10.0"));
        if (IsValidEngineDir(devPath))
            return devPath;

        return null;
    }

    private string? FindDrawerDirectory()
    {
        var baseDir = AppContext.BaseDirectory;

        // Published layout: tools/drawer/
        var publishedPath = Path.Combine(baseDir, "tools", "drawer");
        if (IsValidDrawerDir(publishedPath))
            return publishedPath;

        // Same directory
        if (IsValidDrawerDir(baseDir))
            return baseDir;

        // Development layout
        var devPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..",
            "GeoGen.DrawingLauncher", "bin", "Debug", "net10.0"));
        if (IsValidDrawerDir(devPath))
            return devPath;

        devPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..",
            "GeoGen.DrawingLauncher", "bin", "Release", "net10.0"));
        if (IsValidDrawerDir(devPath))
            return devPath;

        return null;
    }

    private static bool IsValidEngineDir(string dir)
    {
        if (!Directory.Exists(dir)) return false;
        var exeName = GetExecutableName("GeoGen");
        return File.Exists(Path.Combine(dir, exeName))
            || File.Exists(Path.Combine(dir, "GeoGen.dll"));
    }

    private static bool IsValidDrawerDir(string dir)
    {
        if (!Directory.Exists(dir)) return false;
        var exeName = GetExecutableName("GeoGen.DrawingLauncher");
        return File.Exists(Path.Combine(dir, exeName))
            || File.Exists(Path.Combine(dir, "GeoGen.DrawingLauncher.dll"));
    }

    private static string GetExecutableName(string baseName)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{baseName}.exe"
            : baseName;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Thread-safe: queues text into a buffer that is flushed to the UI periodically.
    /// Can be called from any thread.
    /// </summary>
    private void AppendOutput(string text)
    {
        lock (_bufferLock)
        {
            _outputBuffer.Append(text);
        }
        EnsureFlushTimerRunning();
    }

    /// <summary>
    /// Directly sets output on the UI thread (for immediate single updates).
    /// </summary>
    private void SetOutputDirect(string text)
    {
        FlushOutputBuffer(); // flush anything pending first
        OutputText = text;
    }

    private void EnsureFlushTimerRunning()
    {
        if (_flushTimer != null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (_flushTimer != null) return;
            _flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FlushIntervalMs) };
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
            FlushOutputBuffer(); // final flush
        });
    }

    private void FlushOutputBuffer()
    {
        string? chunk;
        lock (_bufferLock)
        {
            if (_outputBuffer.Length == 0) return;
            chunk = _outputBuffer.ToString();
            _outputBuffer.Clear();
        }

        if (chunk == null) return;

        var current = _outputText + chunk;

        // Trim from the front if output is getting too large (prevents ever-growing memory)
        if (current.Length > MaxOutputLength)
        {
            var trimPoint = current.Length - MaxOutputLength;
            var newlineAfterTrim = current.IndexOf('\n', trimPoint);
            if (newlineAfterTrim >= 0)
                current = "[...output trimmed...]\n" + current[(newlineAfterTrim + 1)..];
            else
                current = "[...output trimmed...]\n" + current[trimPoint..];
        }

        OutputText = current;
    }

    private void OpenOutputFolder()
    {
        var engineDir = FindEngineDirectory();
        if (engineDir == null)
        {
            AppendOutput("[ERROR] Engine directory not found.\n");
            return;
        }

        var outputDir = Path.Combine(engineDir, "Examples", "Output");
        if (!Directory.Exists(outputDir))
        {
            AppendOutput("[ERROR] Output directory does not exist yet. Run generation first.\n");
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", outputDir);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", outputDir);
            else
                Process.Start("xdg-open", outputDir);
        }
        catch (Exception ex)
        {
            AppendOutput($"[ERROR] Could not open folder: {ex.Message}\n");
        }
    }

    private void ShowQuickStart()
    {
        OutputText = "";
        var text = new StringBuilder();
        text.AppendLine("================================================================================");
        text.AppendLine("  QUICK START GUIDE");
        text.AppendLine("================================================================================");
        text.AppendLine();
        text.AppendLine("Planar Geometry Studio generates geometry theorems from an input configuration.");
        text.AppendLine("It constructs new geometric objects step by step, then discovers and proves");
        text.AppendLine("relationships (theorems) that hold in the resulting figures.");
        text.AppendLine();
        text.AppendLine("--- STEP 1: WRITE AN INPUT CONFIGURATION ---");
        text.AppendLine();
        text.AppendLine("The input editor (left panel) defines what the generator should work with.");
        text.AppendLine("An input file has this structure:");
        text.AppendLine();
        text.AppendLine("    Constructions:");
        text.AppendLine();
        text.AppendLine("     IntersectionOfLinesFromPoints");
        text.AppendLine("     Median");
        text.AppendLine();
        text.AppendLine("    Initial configuration:");
        text.AppendLine();
        text.AppendLine("     Triangle: A, B, C");
        text.AppendLine("     D = Incenter(A, B, C)");
        text.AppendLine();
        text.AppendLine("    Iterations: 1");
        text.AppendLine("    MaximalPoints: 1");
        text.AppendLine("    MaximalLines: 0");
        text.AppendLine("    MaximalCircles: 0");
        text.AppendLine("    SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric");
        text.AppendLine();
        text.AppendLine("  Constructions:       List of constructions the generator may apply.");
        text.AppendLine("  Initial configuration:  The starting figure (base shape + pre-built objects).");
        text.AppendLine("  Iterations:           Number of generation steps (more = slower, richer).");
        text.AppendLine("  MaximalPoints/Lines/Circles:  Limits on new objects added per iteration.");
        text.AppendLine("  SymmetryGenerationMode:  Controls whether asymmetric results are kept.");
        text.AppendLine();
        text.AppendLine("--- STEP 2: GENERATE PROBLEMS ---");
        text.AppendLine();
        text.AppendLine("Press the 'Generate Problems' button (or F5). The engine reads your input,");
        text.AppendLine("builds configurations, discovers theorems, ranks them, and writes results");
        text.AppendLine("to the output directory. Progress appears in this console panel.");
        text.AppendLine();
        text.AppendLine("--- STEP 3: GENERATE FIGURES ---");
        text.AppendLine();
        text.AppendLine("After generation completes, press 'Generate Figures'. The application");
        text.AppendLine("automatically finds the JSON output, renders EPS figures using MetaPost,");
        text.AppendLine("and converts them to PDF in a folder you choose.");
        text.AppendLine();
        text.AppendLine("Requirements: TeX Live or MiKTeX must be installed for figure generation.");
        text.AppendLine();
        text.AppendLine("--- STEP 4: REVIEW RESULTS ---");
        text.AppendLine();
        text.AppendLine("Use 'Open Output Folder' to browse the generated results:");
        text.AppendLine("  ReadableWithoutProofs/  - Human-readable theorem statements");
        text.AppendLine("  ReadableWithProofs/     - Theorems with full proofs");
        text.AppendLine("  JsonOutput/             - Machine-readable JSON output");
        text.AppendLine("  ReadableBestTheorems/   - Top-ranked theorems");
        text.AppendLine();
        text.AppendLine("--- TIPS ---");
        text.AppendLine();
        text.AppendLine("  - Start with 1 iteration and small limits to verify your configuration.");
        text.AppendLine("  - For richer initial configs (Quadrilateral, many pre-built objects),");
        text.AppendLine("    increase MaximalPoints/Lines/Circles (try 2-3). With more starting");
        text.AppendLine("    objects, the prover can trivially prove results from just 1 new object.");
        text.AppendLine("  - Use Help > Reference for the full list of constructions and base types.");
        text.AppendLine("  - Save your input files (Ctrl+S) to reuse them later.");
        text.AppendLine();
        text.AppendLine("================================================================================");
        AppendOutput(text.ToString());
        ScrollOutputToTop = true;
        StatusText = "Quick Start guide displayed";
    }

    private void ShowReference()
    {
        OutputText = "";
        var text = new StringBuilder();
        text.AppendLine("================================================================================");
        text.AppendLine("  REFERENCE: CONSTRUCTIONS, BASE TYPES, AND PARAMETERS");
        text.AppendLine("================================================================================");
        text.AppendLine();
        text.AppendLine("=== BASE TYPES (Initial Configuration) ===");
        text.AppendLine();
        text.AppendLine("  Triangle: A, B, C              Three non-collinear points (acute)");
        text.AppendLine("  RightTriangle: A, B, C         Right angle at the first point");
        text.AppendLine("  Quadrilateral: A, B, C, D      Four points, convex, no three collinear");
        text.AppendLine("  CyclicQuadrilateral: A, B, C, D  Four concyclic points");
        text.AppendLine("  LineSegment: A, B              Two distinct points");
        text.AppendLine("  LineAndPoint: l, A             A line and a point not on it");
        text.AppendLine("  LineAndTwoPoints: l, A, B      A line and two points not on it");
        text.AppendLine();
        text.AppendLine("=== PREDEFINED CONSTRUCTIONS (13) ===");
        text.AppendLine();
        text.AppendLine("  CenterOfCircle(c)                            Center of circle c");
        text.AppendLine("  Circumcircle(A, B, C)                        Circumscribed circle of ABC");
        text.AppendLine("  CircleWithCenterThroughPoint(A, B)            Circle centered at A through B");
        text.AppendLine("  InternalAngleBisector(A, B, C)                Bisector of angle BAC");
        text.AppendLine("  IntersectionOfLines(l, m)                     Intersection of lines l and m");
        text.AppendLine("  LineFromPoints(A, B)                          Line through A and B");
        text.AppendLine("  Midpoint(A, B)                                Midpoint of segment AB");
        text.AppendLine("  ParallelLine(A, l)                            Line through A parallel to l");
        text.AppendLine("  PerpendicularLine(A, l)                       Line through A perpendicular to l");
        text.AppendLine("  PerpendicularProjection(A, l)                 Foot of perpendicular from A to l");
        text.AppendLine("  PointReflection(A, B)                         Reflection of A through B");
        text.AppendLine("  SecondIntersectionOfCircleAndLineFromPoints(A, B, C, D)");
        text.AppendLine("      Second intersection of line AB with circumcircle of ACD");
        text.AppendLine("  SecondIntersectionOfTwoCircumcircles(A, B, C, D, E)");
        text.AppendLine("      Second intersection of circumcircles of ABC and ADE");
        text.AppendLine();
        text.AppendLine("=== COMPOSED CONSTRUCTIONS (24) ===");
        text.AppendLine();
        text.AppendLine("  Centroid(A, B, C)                             Centroid of triangle ABC");
        text.AppendLine("  CircleWithDiameter(A, B)                      Circle with diameter AB");
        text.AppendLine("  Circumcenter(A, B, C)                         Circumcenter of triangle ABC");
        text.AppendLine("  Excenter(A, B, C)                             A-excenter of triangle ABC");
        text.AppendLine("  Excircle(A, B, C)                             A-excircle of triangle ABC");
        text.AppendLine("  ExternalAngleBisector(A, B, C)                External bisector of angle BAC");
        text.AppendLine("  Incenter(A, B, C)                             Incenter of triangle ABC");
        text.AppendLine("  Incircle(A, B, C)                             Incircle of triangle ABC");
        text.AppendLine("  IntersectionOfLineAndLineFromPoints(l, A, B)  Intersection of l and line AB");
        text.AppendLine("  IntersectionOfLinesFromPoints(A, B, C, D)     Intersection of lines AB and CD");
        text.AppendLine("  IsoscelesTrapezoidPoint(A, B, C)              D such that ABCD is isosc. trap.");
        text.AppendLine("  LineThroughCircumcenter(A, B, C)              Line through A and circumcenter");
        text.AppendLine("  Median(A, B, C)                               A-median of triangle ABC");
        text.AppendLine("  Midline(A, B, C)                              A-midline of triangle ABC");
        text.AppendLine("  MidpointOfArc(A, B, C)                        Midpoint of arc BAC");
        text.AppendLine("  MidpointOfOppositeArc(A, B, C)                Midpoint of arc BC not cont. A");
        text.AppendLine("  NinePointCircle(A, B, C)                      Nine-point circle of ABC");
        text.AppendLine("  OppositePointOnCircumcircle(A, B, C)          Diametrically opposite A on circ.");
        text.AppendLine("  Orthocenter(A, B, C)                          Orthocenter of triangle ABC");
        text.AppendLine("  ParallelLineToLineFromPoints(A, B, C)         Line through A parallel to BC");
        text.AppendLine("  ParallelogramPoint(A, B, C)                   D such that ABDC is parallelogram");
        text.AppendLine("  PerpendicularBisector(A, B)                   Perpendicular bisector of AB");
        text.AppendLine("  PerpendicularLineAtPointOfLine(A, B)          Line at A perpendicular to AB");
        text.AppendLine("  PerpendicularLineToLineFromPoints(A, B, C)    Line through A perp. to BC");
        text.AppendLine("  PerpendicularProjectionOnLineFromPoints(A, B, C)");
        text.AppendLine("      Projection of A onto line BC");
        text.AppendLine("  ReflectionInLine(l, A)                        Reflection of A in line l");
        text.AppendLine("  ReflectionInLineFromPoints(A, B, C)           Reflection of A in line BC");
        text.AppendLine("  TangentLine(A, B, C)                          Tangent to circumcircle at A");
        text.AppendLine();
        text.AppendLine("=== PARAMETERS ===");
        text.AppendLine();
        text.AppendLine("  Iterations: <int>          Number of generation steps");
        text.AppendLine("  MaximalPoints: <int>       Max new points per iteration");
        text.AppendLine("  MaximalLines: <int>        Max new lines per iteration");
        text.AppendLine("  MaximalCircles: <int>      Max new circles per iteration");
        text.AppendLine();
        text.AppendLine("  NOTE: These limits control how many new objects are added per step.");
        text.AppendLine("  With richer starting configurations (e.g. Quadrilateral + several");
        text.AppendLine("  pre-built objects), the engine's theorem prover can often derive all");
        text.AppendLine("  results trivially when only 1 new object is added. In that case,");
        text.AppendLine("  increase these values (try 2-3) so that interactions between multiple");
        text.AppendLine("  new objects produce theorems the prover cannot easily prove.");
        text.AppendLine();
        text.AppendLine("  SymmetryGenerationMode:");
        text.AppendLine("    GenerateBothSymmetricAndAsymmetric   Keep all results (default)");
        text.AppendLine("    GenerateOnlySymmetric                Keep only symmetric results");
        text.AppendLine("    GenerateOnlyFullySymmetric           Keep only fully symmetric results");
        text.AppendLine();
        text.AppendLine("=== DISCOVERED THEOREM TYPES ===");
        text.AppendLine();
        text.AppendLine("  CollinearPoints         Three or more points are collinear");
        text.AppendLine("  ConcyclicPoints         Four or more points are concyclic");
        text.AppendLine("  ConcurrentLines         Three lines meet at one point");
        text.AppendLine("  ParallelLines           Two lines are parallel");
        text.AppendLine("  PerpendicularLines      Two lines are perpendicular");
        text.AppendLine("  TangentCircles          Two circles are tangent");
        text.AppendLine("  LineTangentToCircle     A line is tangent to a circle");
        text.AppendLine("  EqualLineSegments       Two segments have equal length");
        text.AppendLine("  EqualObjects            Two constructions yield the same object");
        text.AppendLine("  Incidence               A point lies on a line or circle");
        text.AppendLine();
        text.AppendLine("=== INPUT FILE SYNTAX ===");
        text.AppendLine();
        text.AppendLine("    Constructions:");
        text.AppendLine();
        text.AppendLine("     <Name1>");
        text.AppendLine("     <Name2>");
        text.AppendLine();
        text.AppendLine("    Initial configuration:");
        text.AppendLine();
        text.AppendLine("     <BaseType>: <P1>, <P2>, ...");
        text.AppendLine("     <Name> = <Construction>(<Arg1>, <Arg2>, ...)");
        text.AppendLine();
        text.AppendLine("    Iterations: <int>");
        text.AppendLine("    MaximalPoints: <int>");
        text.AppendLine("    MaximalLines: <int>");
        text.AppendLine("    MaximalCircles: <int>");
        text.AppendLine("    SymmetryGenerationMode: <mode>");
        text.AppendLine();
        text.AppendLine("================================================================================");
        AppendOutput(text.ToString());
        ScrollOutputToTop = true;
        StatusText = "Reference displayed";
    }

    private async void ShowAbout()
    {
        var aboutText =
            """
            Planar Geometry Studio v1.0.0

            A desktop application for automated generation of planar
            geometry theorems, based on the GeoGen engine by Patrik Bak.

            Features:
              - Edit input configurations
              - Generate geometry problems and theorems
              - Generate and convert figures (EPS to PDF)

            Requirements for figure generation:
              - TeX Live or MiKTeX (provides MetaPost and epstopdf)

            Built on GeoGen: https://github.com/PatrikBak/GeoGen
            """;

        AppendOutput("\n" + aboutText + "\n");
        StatusText = "Planar Geometry Studio v1.0.0";
    }

    #endregion
}
