using GeoGen.DesktopApp.Models;
using System.Diagnostics;

namespace GeoGen.DesktopApp.Services;

public sealed class ProcessRunner
{
    private readonly Action<string> _onOutput;
    private readonly object _processLock = new();
    private Process? _currentProcess;

    public ProcessRunner(Action<string> onOutput)
    {
        _onOutput = onOutput ?? throw new ArgumentNullException(nameof(onOutput));
    }

    public async Task<ProcessResult> RunAsync(
        string executablePath,
        IEnumerable<string> arguments,
        string workingDirectory,
        IEnumerable<string>? standardInputLines,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInputLines is not null,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        startInfo.Environment["GEOGEN_NO_PAUSE"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        using var process = new Process { StartInfo = startInfo };
        lock (_processLock)
            _currentProcess = process;

        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Could not start '{executablePath}'.");

            var outputLines = new List<string>();
            var errorLines = new List<string>();
            var outputTask = ReadLinesAsync(process.StandardOutput, outputLines);
            var errorTask = ReadLinesAsync(process.StandardError, errorLines);

            if (standardInputLines is not null)
            {
                foreach (var line in standardInputLines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await process.StandardInput.WriteLineAsync(line);
                }

                process.StandardInput.Close();
            }

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                await WaitForExitAfterCancellationAsync(process);
                await ObserveReadersAfterCancellationAsync(outputTask, errorTask);
                throw;
            }

            await Task.WhenAll(outputTask, errorTask);
            return new ProcessResult(process.ExitCode, outputLines, errorLines);
        }
        finally
        {
            lock (_processLock)
            {
                if (ReferenceEquals(_currentProcess, process))
                    _currentProcess = null;
            }
        }
    }

    public void CancelCurrent()
    {
        Process? process;
        lock (_processLock)
            process = _currentProcess;

        if (process is not null)
            TryKill(process);
    }

    private async Task ReadLinesAsync(StreamReader reader, List<string> destination)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            destination.Add(line);
            _onOutput(line + Environment.NewLine);
        }
    }

    private static async Task WaitForExitAfterCancellationAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync();
        }
        catch (InvalidOperationException)
        {
            // The process may not have started if cancellation raced with startup.
        }
    }

    private static async Task ObserveReadersAfterCancellationAsync(params Task[] readers)
    {
        try
        {
            await Task.WhenAll(readers);
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // Killing the process can close redirected streams while a read is in flight.
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may already have exited between the check and the kill.
        }
    }
}
