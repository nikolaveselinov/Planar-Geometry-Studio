namespace GeoGen.DesktopApp.Services;

internal static class HelpContent
{
    public const string QuickStart =
        """
        PLANAR GEOMETRY STUDIO — QUICK START

        1. Describe the search space

        Use the editor to choose constructions, an initial configuration, and generation limits.
        File → New restores a complete working example.

        2. Generate theorems

        Press F5 or choose Generate. The Studio validates the configuration, starts the GeoGen
        engine, and streams progress into the console. Every run is stored in its own timestamped
        folder, so an old result is never overwritten.

        3. Inspect the result

        Open Results shows the latest run. Its Output folder contains:

          • ReadableWithoutProofs — concise theorem statements
          • ReadableWithProofs — statements with generated proofs
          • JsonOutput — machine-readable results
          • ReadableBestTheorems — the highest-ranked theorem of each type
          • JsonBestTheorems — machine-readable best theorems

        4. Draw figures

        Choose Draw Figures after a successful run. The Studio uses the newest JSON result,
        renders it with MetaPost, and asks where the figures should be saved. Install TeX Live
        or MiKTeX for MetaPost and PDF conversion; if no PDF converter is available, the original
        EPS figures are preserved.

        Practical advice

          • Start with one iteration and small object limits.
          • Increase MaximalPoints, MaximalLines, or MaximalCircles when the search is too narrow.
          • Prefer a targeted construction list: a smaller search is faster and easier to interpret.
          • Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+Shift+S, F5, and Esc are available throughout the app.
        """;

    public const string Reference =
        """
        PLANAR GEOMETRY STUDIO — INPUT REFERENCE

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

        Predefined constructions

          CenterOfCircle(c)
          Circumcircle(A, B, C)
          CircleWithCenterThroughPoint(A, B)
          CircleWithRadius(A, B, C)          center A, radius |BC|
          InternalAngleBisector(A, B, C)
          IntersectionOfLines(l, m)
          LineFromPoints(A, B)
          Midpoint(A, B)
          ParallelLine(A, l)
          PerpendicularLine(A, l)
          PerpendicularProjection(A, l)
          PointReflection(A, B)
          SecondIntersectionOfCircleAndLineFromPoints(A, B, C, D)
          SecondIntersectionOfTwoCircumcircles(A, B, C, D, E)

        Composed constructions

          Centroid(A, B, C)
          CircleWithDiameter(A, B)
          Circumcenter(A, B, C)
          Excenter(A, B, C)
          Excircle(A, B, C)
          ExternalAngleBisector(A, B, C)
          Incenter(A, B, C)
          Incircle(A, B, C)
          IntersectionOfLineAndLineFromPoints(l, A, B)
          IntersectionOfLinesFromPoints(A, B, C, D)
          IsoscelesTrapezoidPoint(A, B, C)
          LineThroughCircumcenter(A, B, C)
          Median(A, B, C)
          Midline(A, B, C)
          MidpointOfArc(A, B, C)
          MidpointOfOppositeArc(A, B, C)
          NinePointCircle(A, B, C)
          OppositePointOnCircumcircle(A, B, C)
          Orthocenter(A, B, C)
          ParallelLineToLineFromPoints(A, B, C)
          ParallelogramPoint(A, B, C)
          PerpendicularBisector(A, B)
          PerpendicularLineAtPointOfLine(A, B)
          PerpendicularLineToLineFromPoints(A, B, C)
          PerpendicularProjectionOnLineFromPoints(A, B, C)
          ReflectionInLine(l, A)
          ReflectionInLineFromPoints(A, B, C)
          TangentLine(A, B, C)

        Parameters

          Iterations                     number of generation steps
          MaximalPoints                  maximum new points per step
          MaximalLines                   maximum new lines per step
          MaximalCircles                 maximum new circles per step

        Symmetry modes

          GenerateBothSymmetricAndAsymmetric
          GenerateOnlySymmetric
          GenerateOnlyFullySymmetric

        Theorem types include collinearity, concurrency, concyclicity, equal line segments,
        incidence, parallel and perpendicular lines, tangent circles, and line-circle tangency.
        """;
}
