# Developer guide

## Build and test

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```bash
dotnet restore Source/GeoGen.sln
dotnet build Source/GeoGen.sln --configuration Release
dotnet test Source/GeoGen.sln --configuration Release
```

Run the desktop application with:

```bash
dotnet run --project Source/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj
```

## Theorem prover integration test

[GeoGen.TheoremProver.IntegrationTest](Source/Tests/IntegrationTests/GeoGen.TheoremProver.IntegrationTest/Program.cs) takes two arguments:

1. the inference-rule directory;
2. the object-introduction-rule directory.

The default paths are in [launchSettings.json](Source/Tests/IntegrationTests/GeoGen.TheoremProver.IntegrationTest/Properties/launchSettings.json).

## Other launchers

| Launcher | Purpose |
|---|---|
| [Configuration Generation](Source/Launchers/GeoGen.ConfigurationGenerationLauncher) | Count generated configurations and measure memory use |
| [Input Generation](Source/Launchers/GeoGen.InputGenerationLauncher) | Generate input files for large runs |
| [Output Merging](Source/Launchers/GeoGen.OutputMergingLauncher) | Merge JSON output directories |

These launchers were used for the experiments in Patrik Bak's [thesis](https://drive.google.com/file/d/1dsaxDCMzlAPfB3e4rd8ut2RuZ_sn2Zm5/view).
