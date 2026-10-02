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
    private readonly AutomaticUpdateService _automaticUpdater = new();
    private bool _isDownloadingUpdate;
    private bool _updateReady;

    public string UpdateActionText => _updateReady ? "Restart to update" :
        _availableRelease?.Package is not null && AutomaticUpdateService.CanUpdate ? "Download update" : "Open release";
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
        DownloadUpdateCommand = new AsyncRelayCommand(UpdateAsync, () => !_isDownloadingUpdate && !IsRunning);
        DismissUpdateCommand = new RelayCommand(_ => ShowUpdateNotice = false);
    }

    public async Task InitializeStudioAsync()
    {
        _automaticUpdater.ConfirmStarted(AppInfo.Version, UpdateService.RuntimeIdentifier, AppContext.BaseDirectory);
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
        if (_isCheckingUpdates || _isDownloadingUpdate)
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
            _updateReady = _automaticUpdater.Pending is { Attempted: false } pending && pending.Version == _availableRelease?.Version && _automaticUpdater.Verify(pending);
            OnPropertyChanged(nameof(UpdateActionText));
            if (_updateReady) UpdateNotice = $"Studio {_availableRelease!.Version} is ready. It will open on your next launch.";
            else if (_availableRelease?.Package is not null && _studioSettings.DownloadUpdatesAutomatically &&
                AutomaticUpdateService.CanUpdate && !_automaticUpdater.HasFailedUpdate(_availableRelease.Version))
                await DownloadUpdateAsync();
            else if (_availableRelease is not null && _automaticUpdater.HasFailedUpdate(_availableRelease.Version))
                UpdateNotice = "The update did not start. Your previous version is still available; download again to retry.";
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

    private async Task UpdateAsync()
    {
        if (_availableRelease is null) return;
        if (_updateReady)
        {
            if (!await ConfirmCloseAsync()) return;
            if (_automaticUpdater.TryLaunchLatest(AppInfo.Version, UpdateService.RuntimeIdentifier, Array.Empty<string>()))
                ((MainWindow)_window).CloseAfterConfirmation();
            else
            {
                _updateReady = false;
                OnPropertyChanged(nameof(UpdateActionText));
                UpdateNotice = "Could not start the update. Your current version is still running; choose Download update to retry.";
            }
        }
        else if (_availableRelease.Package is not null && AutomaticUpdateService.CanUpdate) await DownloadUpdateAsync();
        else
        {
            try { Process.Start(new ProcessStartInfo { FileName = _availableRelease.ReleasePage.AbsoluteUri, UseShellExecute = true }); }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            { UpdateNotice = "Open this release in your browser: " + _availableRelease.ReleasePage; }
        }
    }

    private async Task DownloadUpdateAsync()
    {
        if (_availableRelease is null || _isDownloadingUpdate) return;
        _isDownloadingUpdate = true;
        ShowUpdateNotice = true;
        (DownloadUpdateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        var version = _availableRelease.Version;
        var lastPercent = -1;
        var progress = new Progress<double>(fraction =>
        {
            var percent = (int)(fraction * 100);
            if (percent == lastPercent || !_isDownloadingUpdate) return;
            lastPercent = percent;
            UpdateNotice = percent == 100 ? $"Verifying Studio {version}…" : $"Downloading Studio {version}… {percent}%";
        });
        UpdateNotice = $"Downloading Studio {version}…";
        try
        {
            await _automaticUpdater.PrepareAsync(_availableRelease, progress, _lifecycleCancellation.Token);
            _updateReady = true;
            UpdateNotice = $"Studio {version} is ready. Restart when convenient, or open it on your next launch.";
        }
        catch (OperationCanceledException)
        {
            if (!_lifecycleCancellation.IsCancellationRequested) UpdateNotice = "Update download timed out. Choose Download update to retry.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            System.Net.Http.HttpRequestException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
        {
            UpdateNotice = "Could not prepare the update. Your current version is unchanged; choose Download update to retry.";
        }
        finally
        {
            _isDownloadingUpdate = false;
            OnPropertyChanged(nameof(UpdateActionText));
            (DownloadUpdateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
