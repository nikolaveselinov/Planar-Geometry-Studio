# Input and output formats

## Input

An input file contains a construction list, an initial configuration, and generation limits.

### `Constructions:`

List one construction name per line. These constructions may be used to add objects.

#### Predefined constructions

| Construction | Result |
|---|---|
| `CenterOfCircle(c)` | Center of `c` |
| `Circumcircle(A, B, C)` | Circumcircle of `ABC` |
| `CircleWithCenterThroughPoint(A, B)` | Circle centered at `A` through `B` |
| `CircleWithRadius(A, B, C)` | Circle centered at `A` with radius `BC` |
| `InternalAngleBisector(A, B, C)` | Internal bisector of angle `BAC` |
| `IntersectionOfLines(l, m)` | Intersection of `l` and `m` |
| `LineFromPoints(A, B)` | Line `AB` |
| `Midpoint(A, B)` | Midpoint of `AB` |
| `ParallelLine(A, l)` | Line through `A` parallel to `l` |
| `PerpendicularLine(A, l)` | Line through `A` perpendicular to `l` |
| `PerpendicularProjection(A, l)` | Projection of `A` onto `l` |
| `PointReflection(A, B)` | Reflection of `A` in `B` |
| `SecondIntersectionOfCircleAndLineFromPoints(A, B, C, D)` | Second intersection of `AB` with the circumcircle of `ACD` |
| `SecondIntersectionOfTwoCircumcircles(A, B, C, D, E)` | Second intersection of the circumcircles of `ABC` and `ADE` |

#### Composed constructions

| Construction | Result |
|---|---|
| `Centroid(A, B, C)` | Centroid of `ABC` |
| `CircleWithDiameter(A, B)` | Circle with diameter `AB` |
| `Circumcenter(A, B, C)` | Circumcenter of `ABC` |
| `Excenter(A, B, C)` | `A`-excenter of `ABC` |
| `Excircle(A, B, C)` | `A`-excircle of `ABC` |
| `ExternalAngleBisector(A, B, C)` | External bisector of angle `BAC` |
| `Incenter(A, B, C)` | Incenter of `ABC` |
| `Incircle(A, B, C)` | Incircle of `ABC` |
| `IntersectionOfLineAndLineFromPoints(l, A, B)` | Intersection of `l` and `AB` |
| `IntersectionOfLinesFromPoints(A, B, C, D)` | Intersection of `AB` and `CD` |
| `IsoscelesTrapezoidPoint(A, B, C)` | Point `D` such that `ABCD` is an isosceles trapezoid |
| `LineThroughCircumcenter(A, B, C)` | Line through `A` and the circumcenter of `ABC` |
| `Median(A, B, C)` | `A`-median of `ABC` |
| `Midline(A, B, C)` | `A`-midline of `ABC` |
| `MidpointOfArc(A, B, C)` | Midpoint of arc `BAC` |
| `MidpointOfOppositeArc(A, B, C)` | Midpoint of the arc `BC` not containing `A` |
| `NinePointCircle(A, B, C)` | Nine-point circle of `ABC` |
| `OppositePointOnCircumcircle(A, B, C)` | Point opposite `A` on the circumcircle of `ABC` |
| `Orthocenter(A, B, C)` | Orthocenter of `ABC` |
| `ParallelLineToLineFromPoints(A, B, C)` | Line through `A` parallel to `BC` |
| `ParallelogramPoint(A, B, C)` | Point `D` such that `ABDC` is a parallelogram |
| `PerpendicularBisector(A, B)` | Perpendicular bisector of `AB` |
| `PerpendicularLineAtPointOfLine(A, B)` | Line through `A` perpendicular to `AB` |
| `PerpendicularLineToLineFromPoints(A, B, C)` | Line through `A` perpendicular to `BC` |
| `PerpendicularProjectionOnLineFromPoints(A, B, C)` | Projection of `A` onto `BC` |
| `ReflectionInLine(l, A)` | Reflection of `A` in `l` |
| `ReflectionInLineFromPoints(A, B, C)` | Reflection of `A` in `BC` |
| `TangentLine(A, B, C)` | Tangent to the circumcircle of `ABC` at `A` |

### `Initial configuration:`

The first line gives a base layout. Later lines may define more objects.

| Layout | Conditions |
|---|---|
| `LineSegment: A, B` | Two distinct points |
| `Triangle: A, B, C` | Three non-collinear points |
| `RightTriangle: A, B, C` | Right angle at `A` |
| `Quadrilateral: A, B, C, D` | No three points collinear |
| `CyclicQuadrilateral: A, B, C, D` | Four concyclic points; no three collinear |
| `LineAndPoint: l, A` | `A` is not on `l` |
| `LineAndTwoPoints: l, A, B` | `A` and `B` are distinct and not on `l` |

Additional objects use the form:

```text
D = Incenter(A, B, C)
```

### Parameters

| Parameter | Meaning |
|---|---|
| `Iterations` | Number of generation steps |
| `MaximalPoints` | Maximum new points per step |
| `MaximalLines` | Maximum new lines per step |
| `MaximalCircles` | Maximum new circles per step |
| `SymmetryGenerationMode` | Symmetry filter |

The symmetry modes are:

- `GenerateBothSymmetricAndAsymmetric`;
- `GenerateOnlySymmetric`;
- `GenerateOnlyFullySymmetric`.

### Example

```text
Constructions:

 IntersectionOfLinesFromPoints
 Median

Initial configuration:

 Triangle: A, B, C
 D = Incenter(A, B, C)

Iterations: 1
MaximalPoints: 1
MaximalLines: 1
MaximalCircles: 0
SymmetryGenerationMode: GenerateBothSymmetricAndAsymmetric
```

More examples are in [Examples/Inputs](Source/Launchers/GeoGen.MainLauncher/Examples/Inputs).

## Output

| Directory | Format | Contents |
|---|---|---|
| `ReadableWithoutProofs` | Text | Theorems not excluded by the analyzer |
| `ReadableWithProofs` | Text | Theorems and proof data |
| `ReadableBestTheorems` | Text | Highest-ranked theorems by type |
| `JsonOutput` | JSON | Theorems not excluded by the analyzer |
| `JsonBestTheorems` | JSON | Highest-ranked theorems by type |

### Theorem types

- `CollinearPoints`
- `ConcyclicPoints`
- `ConcurrentLines`
- `EqualLineSegments`
- `EqualObjects`
- `Incidence`
- `LineTangentToCircle`
- `ParallelLines`
- `PerpendicularLines`
- `TangentCircles`

### JSON

JSON output is an array of objects of the following form:

```json
{
  "TheoremString": "ParallelLines: [A, B], [C, D]",
  "Ranking": {
    "Rankings": {
      "Symmetry": {
        "Ranking": 1.0,
        "Weight": 10000,
        "Contribution": 10000
      }
    },
    "TotalRanking": 10000
  },
  "ConfigurationString": "..."
}
```

`TheoremString` stores the theorem. `ConfigurationString` stores its configuration. `Ranking` stores the component scores, weights, contributions, and total.
