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

    public static IReadOnlySet<string> GetEnabled(string input)
    {
        var (start, end) = FindSection(input);
        var names = input[start..end].Split('\n').Select(line => line.Trim()).ToHashSet(StringComparer.Ordinal);
        return Entries.Where(entry => names.Contains(entry.Name)).Select(entry => entry.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static string Enable(string input, string name) => SetEnabled(input, name, true);

    public static string SetEnabled(string input, string name, bool enabled)
    {
        if (!Entries.Any(entry => entry.Name == name))
            throw new ArgumentException("Choose a construction from the catalog.", nameof(name));
        var (start, end) = FindSection(input);
        var section = input[start..end];
        var entry = new Regex($@"(?m)^[\t ]*{Regex.Escape(name)}[\t ]*\r?(?:\n|$)");
        if (!enabled)
            return input[..start] + entry.Replace(section, string.Empty) + input[end..];
        if (entry.IsMatch(section))
            return input;
        var newline = input.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return input.Insert(start, $"{newline} {name}");
    }

    private static (int Start, int End) FindSection(string input)
    {
        var start = Regex.Match(input, @"(?m)^[\t ]*Constructions:[\t ]*(?=\r?$)");
        var end = Regex.Match(input, @"(?m)^[\t ]*Initial configuration:[\t ]*(?=\r?$)");
        if (!start.Success || !end.Success || end.Index <= start.Index)
            throw new ArgumentException("Add Constructions: before Initial configuration: in your input.", nameof(input));
        return (start.Index + start.Length, end.Index);
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
