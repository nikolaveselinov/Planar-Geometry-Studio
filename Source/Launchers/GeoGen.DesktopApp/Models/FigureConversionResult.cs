namespace GeoGen.DesktopApp.Models;

public sealed record FigureConversionResult(
    int SourceCount,
    int ConvertedCount,
    int EpsFallbackCount,
    IReadOnlyList<string> FailedFiles);
