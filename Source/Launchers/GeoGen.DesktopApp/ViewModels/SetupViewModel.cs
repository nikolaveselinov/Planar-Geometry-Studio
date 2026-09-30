using Avalonia.Controls;
using Avalonia.Threading;
using GeoGen.DesktopApp.Helpers;
using GeoGen.DesktopApp.Services;
using System.Diagnostics;
using System.Windows.Input;

namespace GeoGen.DesktopApp.ViewModels;

public sealed class SetupViewModel : ViewModelBase, IDisposable
{
    private readonly Window _window;
    private readonly StudioSettings _settings;
    private CancellationTokenSource _cancellation = new();
    private bool _isInstalling;
    private bool _drawingConsent;
    private bool _checkForUpdates;
    private string _toolStatus = "Checking installed drawing tools…";
    private string _log = "";
    private string _message = "Generation and proofs are ready. Add drawing tools whenever you need figures.";

    public SetupViewModel(Window window, StudioSettings settings, bool recommendDrawingTools)
    {
        _window = window;
        _settings = settings;
        _checkForUpdates = settings.CheckForUpdatesOnLaunch;
        _drawingConsent = recommendDrawingTools;
        InstallCommand = new AsyncRelayCommand(InstallAsync, () => !IsInstalling && DrawingConsent);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsInstalling);
        ContinueCommand = new RelayCommand(_ => Finish(), _ => !IsInstalling);
        CancelInstallCommand = new RelayCommand(_ => _cancellation.Cancel(), _ => IsInstalling);
        LicenseCommand = new RelayCommand(_ => OpenLink(AppInfo.RepositoryUrl + "/blob/master/LICENSE"));
        MiKTeXLicenseCommand = new RelayCommand(_ => OpenLink("https://miktex.org/copying"));
        TeXLicenseCommand = new RelayCommand(_ => OpenLink("https://tug.org/texlive/copying.html"));
        GhostscriptLicenseCommand = new RelayCommand(_ => OpenLink("https://www.ghostscript.com/licensing/"));
    }

    public string PlatformTools => OperatingSystem.IsWindows() ? "MiKTeX · MetaPost · PDF export"
        : OperatingSystem.IsMacOS() ? "BasicTeX · MetaPost · PDF export" : "TeX Live · MetaPost · Ghostscript";
    public string Terms
    {
        get
        {
            using var stream = typeof(SetupViewModel).Assembly.GetManifestResourceStream("GeoGen.DesktopApp.Terms.md");
            if (stream is null)
                return "Terms are available in TERMS.md beside the application.";
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public bool CheckForUpdates { get => _checkForUpdates; set => SetProperty(ref _checkForUpdates, value); }
    public bool DrawingConsent
    {
        get => _drawingConsent;
        set
        {
            SetProperty(ref _drawingConsent, value);
            (InstallCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }
    public bool IsInstalling
    {
        get => _isInstalling;
        private set
        {
            SetProperty(ref _isInstalling, value);
            OnPropertyChanged(nameof(IsIdle));
            (InstallCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (RefreshCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ContinueCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelInstallCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
    public bool IsIdle => !IsInstalling;
    public string ToolStatus { get => _toolStatus; private set => SetProperty(ref _toolStatus, value); }
    public string Log { get => _log; private set => SetProperty(ref _log, value); }
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public ICommand InstallCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ContinueCommand { get; }
    public ICommand CancelInstallCommand { get; }
    public ICommand LicenseCommand { get; }
    public ICommand MiKTeXLicenseCommand { get; }
    public ICommand TeXLicenseCommand { get; }
    public ICommand GhostscriptLicenseCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            var status = await Task.Run(DrawingToolEnvironment.Detect);
            ToolStatus = status.Summary;
            if (status.Ready)
                Message = "Your drawing tools are ready. You can start the studio.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Message = "Could not inspect drawing tools: " + exception.Message;
        }
    }

    private async Task InstallAsync()
    {
        _cancellation.Dispose();
        _cancellation = new CancellationTokenSource();
        IsInstalling = true;
        Log = "";
        Message = "Setting up drawing tools. Downloads may take several minutes; approve any system permission prompt.";
        try
        {
            await DependencySetupService.InstallAsync(text => Dispatcher.UIThread.Post(() =>
            {
                var updated = Log + text;
                Log = updated.Length > 40_000 ? updated[^40_000..] : updated;
            }), _cancellation.Token);
            await RefreshAsync();
            if (!DrawingToolEnvironment.Detect().Ready)
                Message = "Setup finished, but some tools are still missing. Review the log and select Check again.";
        }
        catch (OperationCanceledException)
        {
            Message = "Setup stopped. You can start Studio and retry from Help → Studio Setup.";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            Message = exception.Message;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    public async Task InstallRecommendedAsync()
    {
        await RefreshAsync();
        if (!DrawingToolEnvironment.Detect().Ready)
            await InstallAsync();
    }

    private void Finish()
    {
        _settings.CheckForUpdatesOnLaunch = CheckForUpdates;
        _settings.SetupCompleted = true;
        try
        {
            _settings.Save();
            _window.Close();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Message = "Could not save preferences: " + exception.Message;
        }
    }

    private void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Message = "Open this link in your browser: " + url;
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}
