namespace GeoGen.DesktopApp.Models;

public sealed record ToolLocation(
    string ExecutablePath,
    string WorkingDirectory,
    IReadOnlyList<string> PrefixArguments);
