using System.Reflection;

namespace GeoGen.DesktopApp;

internal static class AppInfo
{
    public const string Name = "Planar Geometry Studio";
    public const string RepositoryUrl = "https://github.com/nikolaveselinov/Planar-Geometry-Studio";
    public const string GeoGenRepositoryUrl = "https://github.com/PatrikBak/GeoGen";

    public static string Version
    {
        get
        {
            var informationalVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
                return informationalVersion.Split('+')[0];

            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "development";
        }
    }
}
