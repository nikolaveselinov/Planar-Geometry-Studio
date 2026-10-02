using Avalonia;
using GeoGen.DesktopApp.Services;
using System.Text.Json;

namespace GeoGen.DesktopApp;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--updater-smoke")
        {
            var updater = new AutomaticUpdateService(args[1]);
            updater.ConfirmStarted(AppInfo.Version, UpdateService.RuntimeIdentifier, AppContext.BaseDirectory);
            File.WriteAllText(args[2], JsonSerializer.Serialize(new { Version = AppInfo.Version, Directory = AppContext.BaseDirectory }));
            return;
        }
        if (new AutomaticUpdateService().TryLaunchLatest(AppInfo.Version, UpdateService.RuntimeIdentifier, args)) return;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
