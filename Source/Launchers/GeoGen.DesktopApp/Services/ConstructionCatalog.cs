using GeoGen.Core;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeoGen.DesktopApp.Services;

public sealed record ConstructionEntry(string Name, string OutputType, string Invocation,
    string ArgumentTypes, string Description);

public static class ConstructionCatalog
{
    public static IReadOnlyList<ConstructionEntry> Entries { get; } = Load();

    public static IReadOnlyList<ConstructionEntry> Search(string query, string outputType = "All")
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Entries.Where(entry => (outputType == "All" || entry.OutputType == outputType) &&
            words.All(word => $"{entry.Name} {entry.Description} {entry.Invocation} {entry.ArgumentTypes}"
                .Contains(word, StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    public static string Enable(string input, string name)
    {
        if (!Entries.Any(entry => entry.Name == name))
            throw new ArgumentException("Choose a construction from the catalog.", nameof(name));
        var start = Regex.Match(input, @"(?m)^[\t ]*Constructions:[\t ]*(?=\r?$)");
        var end = Regex.Match(input, @"(?m)^[\t ]*Initial configuration:[\t ]*(?=\r?$)");
        if (!start.Success || !end.Success || end.Index <= start.Index)
            throw new ArgumentException("Add Constructions: before Initial configuration: in your input.", nameof(input));
        var section = input[(start.Index + start.Length)..end.Index];
        if (section.Split('\n').Any(line => line.Trim() == name))
            return input;
        var newline = input.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var insertion = start.Index + start.Length;
        return input.Insert(insertion, $"{newline} {name}");
    }

    private static ConstructionEntry[] Load()
    {
        using var stream = typeof(ConstructionCatalog).Assembly.GetManifestResourceStream(
            "GeoGen.DesktopApp.ConstructionDescriptions.json")
            ?? throw new InvalidOperationException("Construction descriptions are missing.");
        var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException("Construction descriptions are invalid.");
        return Constructions.GetAllConstructions().OrderBy(item => item.Name, StringComparer.Ordinal)
            .Select(item =>
            {
                var points = 0;
                var lines = 0;
                var circles = 0;
                var arguments = item.Signature.ObjectTypes.Select(type => type switch
                {
                    ConfigurationObjectType.Point => ((char)('A' + points++)).ToString(),
                    ConfigurationObjectType.Line => ((char)('l' + lines++)).ToString(),
                    ConfigurationObjectType.Circle => ((char)('c' + circles++)).ToString(),
                    _ => throw new InvalidOperationException($"Unknown object type: {type}")
                });
                return new ConstructionEntry(item.Name, item.OutputType.ToString(),
                    $"{item.Name}({string.Join(", ", arguments)})",
                    string.Join(", ", item.Signature.ObjectTypes), descriptions[item.Name]);
            }).ToArray();
    }
}
