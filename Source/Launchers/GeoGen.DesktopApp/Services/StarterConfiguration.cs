namespace GeoGen.DesktopApp.Services;

public static class StarterConfiguration
{
    public const string Text =
        """
        # A triangle and its medians. Press F5 to explore one new point.
        # Browse Constructions to choose other tools for generation.
        Constructions:

         Midpoint
         IntersectionOfLines

        Initial configuration:

         Triangle: A, B, C
         ma = Median(A, B, C)
         mb = Median(B, A, C)
         mc = Median(C, A, B)

        Iterations: 1
        MaximalPoints: 1
        MaximalLines: 0
        MaximalCircles: 0
        SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric
        """;
}
