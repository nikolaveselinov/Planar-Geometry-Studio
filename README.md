# Planar Geometry Studio

A desktop application for automated generation of planar geometry theorems, built on the [GeoGen engine](https://github.com/PatrikBak/GeoGen) by **Patrik Bak**.

Planar Geometry Studio wraps GeoGen in an intuitive GUI that lets you:

- **Write** input configurations in a built-in code editor
- **Generate** geometry problems and ranked theorems with one click
- **Render** publication-quality figures (EPS → PDF) via MetaPost
- **Browse** results in human-readable and machine-readable formats

---

## Download

Pre-built releases are available on the [Releases](../../releases) page.

---

## Usage

### Step 1: Write an Input Configuration

The left panel is a code editor where you define what the generator should work with.

```
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
```

### Step 2: Generate Problems

Press **Generate Problems** (or **F5**). The engine reads your input, builds configurations, discovers theorems, ranks them, and writes results to the output directory.

### Step 3: Generate Figures

After generation completes, press **Generate Figures**. The application automatically locates the JSON output, renders EPS figures using MetaPost, and converts them to PDF in a folder you choose.

> **Requirement:** A TeX distribution (TeX Live or MiKTeX) must be installed for figure generation.

### Step 4: Review Results

Use **Open Output Folder** to browse the generated results:

| Folder | Contents |
|---|---|
| `ReadableWithoutProofs/` | Human-readable theorem statements |
| `ReadableWithProofs/` | Theorems with full proofs |
| `JsonOutput/` | Machine-readable JSON output |
| `ReadableBestTheorems/` | Top-ranked theorems |

---

## Constructions

### Predefined Constructions (13)

| Construction | Description |
|---|---|
| `CenterOfCircle(c)` | Center of circle *c* |
| `Circumcircle(A, B, C)` | Circumscribed circle of *ABC* |
| `CircleWithCenterThroughPoint(A, B)` | Circle centered at *A* through *B* |
| `InternalAngleBisector(A, B, C)` | Bisector of angle *BAC* |
| `IntersectionOfLines(l, m)` | Intersection of lines *l* and *m* |
| `LineFromPoints(A, B)` | Line through *A* and *B* |
| `Midpoint(A, B)` | Midpoint of segment *AB* |
| `ParallelLine(A, l)` | Line through *A* parallel to *l* |
| `PerpendicularLine(A, l)` | Line through *A* perpendicular to *l* |
| `PerpendicularProjection(A, l)` | Foot of perpendicular from *A* to *l* |
| `PointReflection(A, B)` | Reflection of *A* through *B* |
| `SecondIntersectionOfCircleAndLineFromPoints(A, B, C, D)` | Second intersection of line *AB* with circumcircle of *ACD* |
| `SecondIntersectionOfTwoCircumcircles(A, B, C, D, E)` | Second intersection of circumcircles of *ABC* and *ADE* |

### Composed Constructions (28)

| Construction | Description |
|---|---|
| `Centroid(A, B, C)` | Centroid of triangle *ABC* |
| `CircleWithDiameter(A, B)` | Circle with diameter *AB* |
| `Circumcenter(A, B, C)` | Circumcenter of triangle *ABC* |
| `Excenter(A, B, C)` | *A*-excenter of triangle *ABC* |
| `Excircle(A, B, C)` | *A*-excircle of triangle *ABC* |
| `ExternalAngleBisector(A, B, C)` | External bisector of angle *BAC* |
| `Incenter(A, B, C)` | Incenter of triangle *ABC* |
| `Incircle(A, B, C)` | Incircle of triangle *ABC* |
| `IntersectionOfLineAndLineFromPoints(l, A, B)` | Intersection of *l* and line *AB* |
| `IntersectionOfLinesFromPoints(A, B, C, D)` | Intersection of lines *AB* and *CD* |
| `IsoscelesTrapezoidPoint(A, B, C)` | *D* such that *ABCD* is an isosceles trapezoid |
| `LineThroughCircumcenter(A, B, C)` | Line through *A* and the circumcenter of *ABC* |
| `Median(A, B, C)` | *A*-median of triangle *ABC* |
| `Midline(A, B, C)` | *A*-midline of triangle *ABC* |
| `MidpointOfArc(A, B, C)` | Midpoint of arc *BAC* |
| `MidpointOfOppositeArc(A, B, C)` | Midpoint of arc *BC* not containing *A* |
| `NinePointCircle(A, B, C)` | Nine-point circle of *ABC* |
| `OppositePointOnCircumcircle(A, B, C)` | Diametrically opposite *A* on circumcircle |
| `Orthocenter(A, B, C)` | Orthocenter of triangle *ABC* |
| `ParallelLineToLineFromPoints(A, B, C)` | Line through *A* parallel to *BC* |
| `ParallelogramPoint(A, B, C)` | *D* such that *ABDC* is a parallelogram |
| `PerpendicularBisector(A, B)` | Perpendicular bisector of *AB* |
| `PerpendicularLineAtPointOfLine(A, B)` | Line at *A* perpendicular to *AB* |
| `PerpendicularLineToLineFromPoints(A, B, C)` | Line through *A* perpendicular to *BC* |
| `PerpendicularProjectionOnLineFromPoints(A, B, C)` | Projection of *A* onto line *BC* |
| `ReflectionInLine(l, A)` | Reflection of *A* in line *l* |
| `ReflectionInLineFromPoints(A, B, C)` | Reflection of *A* in line *BC* |
| `TangentLine(A, B, C)` | Tangent to circumcircle of *ABC* at *A* |

---

## Parameters

| Parameter | Description |
|---|---|
| `Iterations` | Number of generation steps (more = slower, richer) |
| `MaximalPoints` | Max new points per iteration |
| `MaximalLines` | Max new lines per iteration |
| `MaximalCircles` | Max new circles per iteration |
| `SymmetryGenerationMode` | `GenerateBothSymmetricAndAsymmetric` (default), `GenerateOnlySymmetric`, `GenerateOnlyFullySymmetric` |

### Base Types

| Type | Description |
|---|---|
| `Triangle: A, B, C` | Three non-collinear points (acute) |
| `RightTriangle: A, B, C` | Right angle at the first point |
| `Quadrilateral: A, B, C, D` | Four points, convex, no three collinear |
| `CyclicQuadrilateral: A, B, C, D` | Four concyclic points |
| `LineSegment: A, B` | Two distinct points |
| `LineAndPoint: l, A` | A line and a point not on it |
| `LineAndTwoPoints: l, A, B` | A line and two points not on it |

---

## Build from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [TeX Live](https://tug.org/texlive/) or [MiKTeX](https://miktex.org/) (for figure generation)

### Build

```bash
cd Source
dotnet build GeoGen.sln
```

### Run the Desktop App

```bash
cd Source/Launchers/GeoGen.DesktopApp
dotnet run
```

### Publish (Self-Contained)

**Linux:**
```bash
./publish.sh
```

**Windows (PowerShell):**
```powershell
.\publish.ps1
```

---

## Acknowledgments

This project is built on the **GeoGen** engine by [Patrik Bak](https://github.com/PatrikBak/GeoGen), which implements automated generation of planar geometry theorems. The original GeoGen engine handles configuration generation, theorem discovery, proving, and ranking. Planar Geometry Studio adds a desktop GUI and figure generation workflow on top of this engine.

## License

This project is licensed under the GNU Affero General Public License v3.0 — the same license as the original [GeoGen](https://github.com/PatrikBak/GeoGen) engine. See [LICENSE](LICENSE) for details.
