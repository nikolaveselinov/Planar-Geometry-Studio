namespace GeoGen.DesktopApp.Services;

internal static class HelpContent
{
    public const string QuickStart =
        """
        QUICK START

        1. Try the starter triangle configuration, or write your own.
        2. Press F5 or select Generate.
        3. Select Open Results to view the output.
        4. Select Figures to draw the latest result.

        Browse Constructions (Ctrl+K) to search the complete catalog, copy a call,
        or enable a construction for generation. The starter enables only a small set.

        Each run is stored in a separate folder under:

          Documents/Planar Geometry Studio/Runs/

        Figure generation requires MetaPost from TeX Live or MiKTeX. Without a PDF converter,
        figures are saved as EPS files.

        Shortcuts

          Ctrl+N        New
          Ctrl+O        Open
          Ctrl+S        Save
          Ctrl+Shift+S  Save As
          F5            Generate
          Esc           Stop
        """;

    public const string Reference =
        """
        INPUT REFERENCE

        File structure

          Constructions:

           Median
           IntersectionOfLinesFromPoints

          Initial configuration:

           Triangle: A, B, C
           D = Incenter(A, B, C)

          Iterations: 1
          MaximalPoints: 1
          MaximalLines: 0
          MaximalCircles: 0
          SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric

        Base layouts

          Triangle: A, B, C                 three non-collinear points
          RightTriangle: A, B, C            right angle at A
          Quadrilateral: A, B, C, D         four points, no three collinear
          CyclicQuadrilateral: A, B, C, D   four concyclic points
          LineSegment: A, B                 two distinct points
          LineAndPoint: l, A                a line and a point not on it
          LineAndTwoPoints: l, A, B         a line and two points not on it

        Construction browser

          Select Constructions above the input editor, or press Ctrl+K.
          Search by name or description and filter by the type of object created.
          Each entry shows its arguments, example call, and geometric meaning.
          Enable in input adds its name to the generation list without duplicates.
          Copy call copies an example to adapt in your initial configuration.

          The Constructions section controls which tools generate new objects.
          Initial configuration definitions may use any supported construction.
          Object limits count new objects across the whole run, not per step.

        Parameters

          Iterations                     number of generation steps
          MaximalPoints                  maximum new points in the run
          MaximalLines                   maximum new lines in the run
          MaximalCircles                 maximum new circles in the run

        Symmetry modes

          GenerateBothSymmetricAndAsymmetric
          GenerateOnlySymmetric
          GenerateOnlyFullySymmetric

        Theorem types

          CollinearPoints
          ConcurrentLines
          ConcyclicPoints
          EqualLineSegments
          Incidence
          LineTangentToCircle
          ParallelLines
          PerpendicularLines
          TangentCircles
        """;
}
