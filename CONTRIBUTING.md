# Contributing

Install the .NET 10 SDK, then run:

```bash
dotnet restore Source/GeoGen.sln
dotnet build Source/GeoGen.sln --configuration Release
dotnet test Source/GeoGen.sln --configuration Release
```

Run the application with:

```bash
dotnet run --project Source/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj
```

Pull requests should:

- address one change;
- include tests when behavior changes;
- exclude generated output;
- update the documentation when needed;
- pass CI.

Contributions are licensed under AGPL-3.0.
