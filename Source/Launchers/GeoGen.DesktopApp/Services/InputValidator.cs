using System.Text.RegularExpressions;

namespace GeoGen.DesktopApp.Services;

public static partial class InputValidator
{
    private static readonly string[] RequiredIntegerParameters =
    {
        "Iterations",
        "MaximalPoints",
        "MaximalLines",
        "MaximalCircles"
    };

    private static readonly HashSet<string> SupportedSymmetryModes = new(StringComparer.Ordinal)
    {
        "GenerateBothSymmetricAndAsymmetric",
        "GenerateOnlySymmetric",
        "GenerateOnlyFullySymmetric"
    };

    public static IReadOnlyList<string> Validate(string input)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input))
            return new[] { "Input is empty." };

        RequireSection(input, "Constructions:", errors);
        RequireSection(input, "Initial configuration:", errors);

        foreach (var parameter in RequiredIntegerParameters)
        {
            var match = Regex.Match(input, $@"(?m)^\s*{Regex.Escape(parameter)}\s*:\s*(-?\d+)\s*$");
            if (!match.Success)
            {
                errors.Add($"Missing or invalid '{parameter}' value.");
                continue;
            }

            if (!int.TryParse(match.Groups[1].Value, out var value) || value < 0)
                errors.Add($"'{parameter}' must be a non-negative integer.");
        }

        var symmetryMatch = SymmetryModeRegex().Match(input);
        if (!symmetryMatch.Success)
        {
            errors.Add("Missing 'SymmetryGenerationMode' parameter.");
        }
        else if (!SupportedSymmetryModes.Contains(symmetryMatch.Groups[1].Value))
        {
            errors.Add($"Unsupported symmetry mode '{symmetryMatch.Groups[1].Value}'.");
        }

        return errors;
    }

    private static void RequireSection(string input, string sectionName, ICollection<string> errors)
    {
        if (!Regex.IsMatch(input, $@"(?m)^\s*{Regex.Escape(sectionName)}\s*$"))
            errors.Add($"Missing '{sectionName}' section.");
    }

    [GeneratedRegex(@"(?m)^\s*SymmetryGenerationMode\s*:\s*(\S+)\s*$")]
    private static partial Regex SymmetryModeRegex();
}
