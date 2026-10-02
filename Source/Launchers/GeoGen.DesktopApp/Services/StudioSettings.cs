using System.Text.Json;

namespace GeoGen.DesktopApp.Services;

public sealed class StudioSettings
{
    public bool SetupCompleted { get; set; }
    public bool DownloadUpdatesAutomatically { get; set; } = true;
    public bool CheckForUpdatesOnLaunch { get; set; } = true;

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
        "PlanarGeometryStudio", "settings.json");

    public static StudioSettings Load(string? path = null)
    {
        try
        {
            return JsonSerializer.Deserialize<StudioSettings>(File.ReadAllText(path ?? SettingsPath)) ?? new();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Save(string? path = null)
    {
        var destination = path ?? SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this));
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
