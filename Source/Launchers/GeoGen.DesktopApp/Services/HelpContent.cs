namespace GeoGen.DesktopApp.Services;

internal static class HelpContent
{
    public const string QuickStart =
        """
        QUICK START

        1. Write an input configuration.
        2. Press F5 or select Generate.
        3. Select Open Results to view the output.
        4. Select Figures to draw the latest result.

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
