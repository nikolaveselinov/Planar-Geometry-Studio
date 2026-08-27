using GeoGen.DesktopApp.Models;
using System.Runtime.InteropServices;

namespace GeoGen.DesktopApp.Services;

public sealed class ToolLocator
{
    private readonly string _baseDirectory;

    public ToolLocator(string? baseDirectory = null)
    {
        _baseDirectory = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
    }

    public ToolLocation? FindEngine() => FindTool(
        publishedRelativePath: Path.Combine("tools", "engine"),
        projectDirectoryName: "GeoGen.MainLauncher",
        assemblyName: "GeoGen");

    public ToolLocation? FindDrawer() => FindTool(
        publishedRelativePath: Path.Combine("tools", "drawer"),
        projectDirectoryName: "GeoGen.DrawingLauncher",
        assemblyName: "GeoGen.DrawingLauncher");

    private ToolLocation? FindTool(
        string publishedRelativePath,
        string projectDirectoryName,
        string assemblyName)
    {
        var publishedDirectory = Path.Combine(_baseDirectory, publishedRelativePath);
        var published = ResolveFromDirectory(publishedDirectory, assemblyName);
        if (published is not null)
            return published;

        var sibling = ResolveFromDirectory(_baseDirectory, assemblyName);
        if (sibling is not null)
            return sibling;

        var projectDirectory = FindDevelopmentProjectDirectory(projectDirectoryName);
        if (projectDirectory is null)
            return null;

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

    private string? FindDevelopmentProjectDirectory(string projectDirectoryName)
    {
        for (var current = new DirectoryInfo(_baseDirectory); current is not null; current = current.Parent)
        {
            var siblingProject = Path.Combine(current.FullName, projectDirectoryName);
            if (Directory.Exists(siblingProject))
                return siblingProject;

            var launcherProject = Path.Combine(current.FullName, "Launchers", projectDirectoryName);
            if (Directory.Exists(launcherProject))
                return launcherProject;
        }

        return null;
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
