namespace GeoGen.DesktopApp.Services;

public static class DependencySetupService
{
    public static async Task InstallAsync(Action<string> onOutput, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onOutput);
        var directory = Path.Combine(AppContext.BaseDirectory, "setup");
        var script = Path.Combine(directory, OperatingSystem.IsWindows() ? "drawing-tools.ps1" : "drawing-tools.sh");
        if (!File.Exists(script))
            throw new FileNotFoundException("The drawing-tools installer is missing. Reinstall Studio from the latest release.", script);

        var executable = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe")
            : "/bin/bash";
        var arguments = OperatingSystem.IsWindows()
            ? new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script }
            : new[] { script };
        var runner = new ProcessRunner(onOutput);
        var result = await runner.RunAsync(executable, arguments, Path.GetTempPath(), null, cancellationToken);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Drawing-tools setup exited with code {result.ExitCode}. See the log below; you can retry or continue without figures.");
    }
}
