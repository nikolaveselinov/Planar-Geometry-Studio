namespace GeoGen.DesktopApp.Models;

public sealed record ProcessResult(int ExitCode, IReadOnlyList<string> OutputLines, IReadOnlyList<string> ErrorLines)
{
    public IEnumerable<string> AllLines => OutputLines.Concat(ErrorLines);

    public bool Contains(string value) =>
        AllLines.Any(line => line.Contains(value, StringComparison.OrdinalIgnoreCase));
}
