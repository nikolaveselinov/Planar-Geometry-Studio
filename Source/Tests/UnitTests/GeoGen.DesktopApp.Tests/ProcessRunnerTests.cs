using GeoGen.DesktopApp.Services;
using NUnit.Framework;
using System.Runtime.InteropServices;

namespace GeoGen.DesktopApp.Tests;

public sealed class ProcessRunnerTests
{
    [Test]
    public async Task CapturesOutputErrorAndExitCode()
    {
        var streamed = new List<string>();
        var runner = new ProcessRunner(streamed.Add);
        var (executable, arguments) = CreateShellCommand(
            windowsCommand: "echo output & echo error 1>&2 & exit /b 7",
            unixCommand: "printf 'output\\n'; printf 'error\\n' >&2; exit 7");

        var result = await runner.RunAsync(
            executable,
            arguments,
            Environment.CurrentDirectory,
            standardInputLines: null,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(7));
            Assert.That(result.OutputLines, Is.EqualTo(new[] { "output" }));
            Assert.That(result.ErrorLines, Is.EqualTo(new[] { "error" }));
            Assert.That(string.Concat(streamed), Does.Contain("output"));
            Assert.That(string.Concat(streamed), Does.Contain("error"));
        });
    }

    [Test]
    public void CancellationStopsLongRunningProcess()
    {
        var runner = new ProcessRunner(_ => { });
        var (executable, arguments) = CreateShellCommand(
            windowsCommand: "ping 127.0.0.1 -n 30 > nul",
            unixCommand: "sleep 30");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await runner.RunAsync(
                executable,
                arguments,
                Environment.CurrentDirectory,
                standardInputLines: null,
                cancellation.Token));
    }

    [Test]
    public void RespectsCancellationBeforeStartingProcess()
    {
        var runner = new ProcessRunner(_ => { });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await runner.RunAsync(
                "this-command-must-not-run",
                Array.Empty<string>(),
                Environment.CurrentDirectory,
                standardInputLines: null,
                cancellation.Token));
    }

    private static (string Executable, string[] Arguments) CreateShellCommand(
        string windowsCommand,
        string unixCommand) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? ("cmd.exe", new[] { "/d", "/s", "/c", windowsCommand })
            : ("/bin/sh", new[] { "-c", unixCommand });
}
