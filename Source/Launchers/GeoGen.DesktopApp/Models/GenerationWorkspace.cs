namespace GeoGen.DesktopApp.Models;

public sealed record GenerationWorkspace(string RootDirectory)
{
    public string InputDirectory => Path.Combine(RootDirectory, "Input");
    public string OutputDirectory => Path.Combine(RootDirectory, "Output");
    public string LogsDirectory => Path.Combine(RootDirectory, "Logs");
    public string SettingsFilePath => Path.Combine(RootDirectory, "studio.settings.json");
    public string JsonOutputDirectory => Path.Combine(OutputDirectory, "JsonOutput");
    public string ReadableOutputDirectory => Path.Combine(OutputDirectory, "ReadableWithoutProofs");
    public string ReadableOutputWithProofsDirectory => Path.Combine(OutputDirectory, "ReadableWithProofs");
    public string ReadableBestTheoremsDirectory => Path.Combine(OutputDirectory, "ReadableBestTheorems");
    public string JsonBestTheoremsDirectory => Path.Combine(OutputDirectory, "JsonBestTheorems");
}
