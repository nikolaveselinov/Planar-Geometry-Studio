using GeoGen.DesktopApp.Helpers;
using GeoGen.DesktopApp.Services;
using GeoGen.DesktopApp.Views;
using System.Diagnostics;
using System.Windows.Input;

namespace GeoGen.DesktopApp.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly StudioSettings _studioSettings = StudioSettings.Load();
    private readonly CancellationTokenSource _lifecycleCancellation = new();
    private AvailableRelease? _availableRelease;
    private string _updateNotice = "";
    private bool _showUpdateNotice;
    private bool _isCheckingUpdates;
    private bool _setupOpen;

    public string UpdateNotice { get => _updateNotice; private set => SetProperty(ref _updateNotice, value); }
    public bool ShowUpdateNotice { get => _showUpdateNotice; private set => SetProperty(ref _showUpdateNotice, value); }
    public bool HasUpdate => _availableRelease is not null;
    public ICommand StudioSetupCommand { get; private set; } = null!;
    public ICommand CheckForUpdatesCommand { get; private set; } = null!;
    public ICommand DownloadUpdateCommand { get; private set; } = null!;
    public ICommand DismissUpdateCommand { get; private set; } = null!;

    private void InitializeSetupCommands()
    {
        StudioSetupCommand = new AsyncRelayCommand(() => ShowStudioSetupAsync(false), () => !IsRunning);
        CheckForUpdatesCommand = new AsyncRelayCommand(() => CheckForUpdatesAsync(manual: true));
        DownloadUpdateCommand = new RelayCommand(_ => DownloadUpdate());
        DismissUpdateCommand = new RelayCommand(_ => ShowUpdateNotice = false);
    }

    public async Task InitializeStudioAsync()
    {
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Contains("--no-update-checks", StringComparer.Ordinal))
            _studioSettings.CheckForUpdatesOnLaunch = false;
        if (!_studioSettings.SetupCompleted || arguments.Contains("--setup", StringComparer.Ordinal))
            await ShowStudioSetupAsync(arguments.Contains("--install-drawing-tools", StringComparer.Ordinal));

        if (_studioSettings.CheckForUpdatesOnLaunch && !_lifecycleCancellation.IsCancellationRequested)
            await CheckForUpdatesAsync(manual: false);
    }

    private async Task ShowStudioSetupAsync(bool recommendDrawingTools)
    {
        if (_setupOpen)
            return;
        _setupOpen = true;
        try
        {
            var dialog = new SetupWindow(_studioSettings, recommendDrawingTools);
            await dialog.ShowDialog(_window);
        }
        finally
        {
            _setupOpen = false;
        }
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (_isCheckingUpdates)
            return;
        _isCheckingUpdates = true;
        if (manual)
        {
            UpdateNotice = "Checking for updates…";
            ShowUpdateNotice = true;
        }

        try
        {
            var result = await new UpdateService().CheckAsync(AppInfo.Version, UpdateService.RuntimeIdentifier, _lifecycleCancellation.Token);
            _availableRelease = result.Release;
            OnPropertyChanged(nameof(HasUpdate));
            UpdateNotice = result.Message;
            ShowUpdateNotice = manual || HasUpdate;
        }
        catch (OperationCanceledException)
        {
            // Closing the window cancels its network request.
        }
        finally
        {
            _isCheckingUpdates = false;
        }
    }

    private void DownloadUpdate()
    {
        if (_availableRelease is null)
            return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = (_availableRelease.InstallerUrl ?? _availableRelease.ReleasePage).AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            UpdateNotice = "Open this release in your browser: " + _availableRelease.ReleasePage;
        }
    }
}
