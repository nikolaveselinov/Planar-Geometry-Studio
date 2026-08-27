# Contributing

Thank you for improving Planar Geometry Studio. Small, focused pull requests with tests are easiest to review.

## Development setup

You need the .NET 10 SDK. MetaPost from TeX Live or MiKTeX is optional and only required to render figures.

```bash
dotnet restore Source/GeoGen.sln
dotnet build Source/GeoGen.sln --configuration Release
dotnet test Source/GeoGen.sln --configuration Release
```

Run the desktop app with:

```bash
dotnet run --project Source/Launchers/GeoGen.DesktopApp/GeoGen.DesktopApp.csproj
```

Create a self-contained package for the current platform with `./publish.sh`, or pass a runtime such as `./publish.sh win-x64`.

## Pull requests

- Explain the user impact and keep unrelated refactors separate.
- Add or update tests for behavior changes.
- Never commit generated output from GeoGen, MetaPost, build, or packaging runs.
- Update `README.md` and `CHANGELOG.md` when behavior or distribution changes.
- Confirm the CI build, test, and package smoke test pass.

By contributing, you agree that your work is licensed under AGPL-3.0-only.
