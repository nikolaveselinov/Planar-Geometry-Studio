using GeoGen.DesktopApp.Services;
using GeoGen.DesktopApp.Models;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace GeoGen.DesktopApp.Tests;

public sealed class ProcessRunnerTests
{
    [Test]
    public void FailedToolReportsItsExceptionAlongsideTheExitCode()
    {
        var result = new ProcessResult(-532462766, Array.Empty<string>(), new[] { "", "Unhandled exception. LoggingException: Cannot configure logging.", "   at Program.Main()" });
        Assert.That(result.FailureMessage("The drawing tool"), Does.Contain("LoggingException: Cannot configure logging.").And.Contain("-532462766"));
        var silent = new ProcessResult(7, Array.Empty<string>(), Array.Empty<string>());
        Assert.That(silent.FailureMessage("The drawing tool"), Does.Contain("code 7").And.Contain("output above"));
    }

    [Test]
    public async Task CapturesOutputErrorAndExitCode()
    {
        var streamed = new ConcurrentQueue<string>();
        var runner = new ProcessRunner(streamed.Enqueue);
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
    public async Task WritesStandardInputAndClosesTheStream()
    {
        var runner = new ProcessRunner(_ => { });
        var (executable, arguments) = CreateShellCommand(
            windowsCommand: "more",
            unixCommand: "cat");

        var result = await runner.RunAsync(
            executable,
            arguments,
            Environment.CurrentDirectory,
            new[] { "first", "second" },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero);
            Assert.That(result.OutputLines, Is.EqualTo(new[] { "first", "second" }));
            Assert.That(result.ErrorLines, Is.Empty);
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

        Assert.CatchAsync<OperationCanceledException>(async () =>
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

        Assert.CatchAsync<OperationCanceledException>(async () =>
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
