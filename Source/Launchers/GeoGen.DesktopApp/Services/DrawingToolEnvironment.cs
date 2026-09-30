namespace GeoGen.DesktopApp.Services;

public sealed record DrawingToolStatus(string? MetaPost, string? TeX, string? PdfConverter)
{
    public bool Ready => MetaPost is not null && TeX is not null && PdfConverter is not null;
    public string Summary => $"MetaPost: {Describe(MetaPost)}\nTeX: {Describe(TeX)}\nPDF converter: {Describe(PdfConverter)}";
    private static string Describe(string? path) => path ?? "not installed";
}

public static class DrawingToolEnvironment
{
    // GUI applications launched from Finder/Start often inherit an old or minimal PATH.
    // Discover installations directly and pass a fresh PATH to every child process.
    public static IEnumerable<string> SearchDirectories()
    {
        var candidates = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            candidates.AddRange((Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "").Split(Path.PathSeparator));
            candidates.AddRange((Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "").Split(Path.PathSeparator));
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            candidates.Add(Path.Combine(local, "Programs", "MiKTeX", "miktex", "bin", "x64"));
            candidates.Add(Path.Combine(programFiles, "MiKTeX", "miktex", "bin", "x64"));
            var gsRoot = Path.Combine(programFiles, "gs");
            if (Directory.Exists(gsRoot))
                candidates.AddRange(Directory.EnumerateDirectories(gsRoot).OrderDescending(StringComparer.Ordinal).Select(path => Path.Combine(path, "bin")));
        }
        else if (OperatingSystem.IsMacOS())
        {
            candidates.AddRange(new[] { "/Library/TeX/texbin", "/opt/homebrew/bin", "/usr/local/bin", "/usr/bin", "/bin" });
        }
        else
        {
            candidates.AddRange(new[] { "/usr/local/bin", "/usr/bin", "/bin" });
        }
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator));
        return candidates.Where(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path))
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    public static string? FindExecutable(params string[] names) => FindExecutableIn(SearchDirectories(), names);

    public static string? FindExecutableIn(IEnumerable<string> directories, params string[] names)
    {
        foreach (var directory in directories)
        foreach (var name in names)
        {
            var path = Path.Combine(directory, OperatingSystem.IsWindows() && !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name + ".exe" : name);
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    public static DrawingToolStatus Detect() => new(
        FindExecutable("mpost", "miktex-mpost"), FindExecutable("tex", "miktex-tex"),
        OperatingSystem.IsWindows()
            ? FindExecutable("epstopdf", "miktex-epstopdf", "gswin64c", "gswin32c")
            : FindExecutable("gs"));

    public static string BuildPath() => string.Join(Path.PathSeparator, SearchDirectories());
}
