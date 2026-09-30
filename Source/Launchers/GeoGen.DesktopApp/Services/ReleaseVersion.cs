using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace GeoGen.DesktopApp.Services;

public static partial class ReleaseVersion
{
    public static bool IsNewer(string candidate, string current)
    {
        var left = VersionPattern().Match(candidate);
        var right = VersionPattern().Match(current);
        if (!left.Success || !right.Success)
            return false;

        for (var index = 1; index <= 3; index++)
        {
            var comparison = BigInteger.Parse(left.Groups[index].Value, CultureInfo.InvariantCulture)
                .CompareTo(BigInteger.Parse(right.Groups[index].Value, CultureInfo.InvariantCulture));
            if (comparison != 0)
                return comparison > 0;
        }

        var leftPre = left.Groups[4].Value;
        var rightPre = right.Groups[4].Value;
        if (leftPre.Length == 0 || rightPre.Length == 0)
            return leftPre.Length == 0 && rightPre.Length > 0;

        var leftParts = leftPre.Split('.');
        var rightParts = rightPre.Split('.');
        for (var index = 0; index < Math.Min(leftParts.Length, rightParts.Length); index++)
        {
            var leftNumeric = BigInteger.TryParse(leftParts[index], NumberStyles.None, CultureInfo.InvariantCulture, out var a);
            var rightNumeric = BigInteger.TryParse(rightParts[index], NumberStyles.None, CultureInfo.InvariantCulture, out var b);
            var comparison = leftNumeric && rightNumeric ? a.CompareTo(b)
                : leftNumeric != rightNumeric ? (leftNumeric ? -1 : 1)
                : string.CompareOrdinal(leftParts[index], rightParts[index]);
            if (comparison != 0)
                return comparison > 0;
        }

        return leftParts.Length > rightParts.Length;
    }

    [GeneratedRegex(@"^v?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
