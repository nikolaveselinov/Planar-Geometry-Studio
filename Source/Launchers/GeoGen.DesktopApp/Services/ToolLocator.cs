using GeoGen.DesktopApp.Models;
using System.Runtime.InteropServices;

namespace GeoGen.DesktopApp.Services;

public sealed class ToolLocator
{
    public ToolLocation? FindEngine() => FindTool(
        publishedRelativePath: Path.Combine("tools", "engine"),
        projectDirectoryName: "GeoGen.MainLauncher",
        assemblyName: "GeoGen");

    public ToolLocation? FindDrawer() => FindTool(
        publishedRelativePath: Path.Combine("tools", "drawer"),
        projectDirectoryName: "GeoGen.DrawingLauncher",
        assemblyName: "GeoGen.DrawingLauncher");

    private static ToolLocation? FindTool(
        string publishedRelativePath,
        string projectDirectoryName,
        string assemblyName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var publishedDirectory = Path.Combine(baseDirectory, publishedRelativePath);
        var published = ResolveFromDirectory(publishedDirectory, assemblyName);
        if (published is not null)
            return published;

        var sibling = ResolveFromDirectory(baseDirectory, assemblyName);
        if (sibling is not null)
            return sibling;

        var projectDirectory = Path.GetFullPath(Path.Combine(
            baseDirectory, "..", "..", "..", "..", projectDirectoryName));
        var binDirectory = Path.Combine(projectDirectory, "bin");
        if (!Directory.Exists(binDirectory))
            return null;

        var executableName = GetExecutableName(assemblyName);
        var candidates = Directory
            .EnumerateFiles(binDirectory, executableName, SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(binDirectory, $"{assemblyName}.dll", SearchOption.AllDirectories))
            .OrderByDescending(File.GetLastWriteTimeUtc);

        return candidates
            .Select(path => ResolveFromDirectory(Path.GetDirectoryName(path)!, assemblyName))
            .FirstOrDefault(location => location is not null);
    }

    private static ToolLocation? ResolveFromDirectory(string directory, string assemblyName)
    {
        if (!Directory.Exists(directory))
            return null;

        var executablePath = Path.Combine(directory, GetExecutableName(assemblyName));
        if (File.Exists(executablePath))
            return new ToolLocation(executablePath, directory, Array.Empty<string>());

        var dllPath = Path.Combine(directory, $"{assemblyName}.dll");
        if (File.Exists(dllPath))
            return new ToolLocation("dotnet", directory, new[] { dllPath });

        return null;
    }

    private static string GetExecutableName(string assemblyName) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{assemblyName}.exe" : assemblyName;
}
