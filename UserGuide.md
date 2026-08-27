# User guide

## Desktop application

Write a configuration in the editor and select **Generate**. The console shows GeoGen's output. Each run is stored under `Documents/Planar Geometry Studio/Runs/`.

Select **Open Results** to open the latest run. Its output contains:

| Directory | Contents |
|---|---|
| `ReadableWithoutProofs` | Theorem statements |
| `ReadableWithProofs` | Theorems and proofs |
| `ReadableBestTheorems` | Highest-ranked theorems |
| `JsonOutput` | JSON output |
| `JsonBestTheorems` | Highest-ranked theorems in JSON |

The input syntax is described in the [input and output reference](InputOutputFormat.md).

## Figures

Select **Figures** after a successful run. Choose a destination folder when prompted.

Figure generation requires MetaPost from [TeX Live](https://tug.org/texlive/) or [MiKTeX](https://miktex.org/). PDF conversion uses `epstopdf`, MiKTeX's `miktex-epstopdf`, or Ghostscript. Without a converter, the application saves EPS files.

## Command-line launchers

### GeoGen

[GeoGen.MainLauncher](Source/Launchers/GeoGen.MainLauncher) reads `settings.json`. Its default input directory is `Examples/Inputs`; output is written to `Examples/Output`.

Run it with:

```bash
dotnet run --project Source/Launchers/GeoGen.MainLauncher/GeoGen.MainLauncher.csproj
```

### Drawing launcher

[GeoGen.DrawingLauncher](Source/Launchers/GeoGen.DrawingLauncher) reads ranked-theorem JSON and writes MetaPost figures.

```bash
dotnet run --project Source/Launchers/GeoGen.DrawingLauncher/GeoGen.DrawingLauncher.csproj
```

Enter a JSON path, then a theorem number or interval.
