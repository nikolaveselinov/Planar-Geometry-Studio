using System.Diagnostics;

namespace GeoGen.Utilities
{
    /// <summary>
    /// The utilities related to running <see cref="Process"/>es.
    /// </summary>
    public static class ProcessUtilities
    {
        /// <summary>
        /// A helper method that runs a given command with arguments asynchronously.
        /// </summary>
        /// <param name="command">The command to be run.</param>
        /// <param name="arguments">The arguments of the command.</param>
        /// <returns>The exit code, the output from the command's output stream, the output from the command's error stream.</returns>
        public static async Task<(int exitCode, string outputData, string errorData)> RunCommandAsync(string command, string arguments, string workingDirectory = null)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory ?? string.Empty
                }
            };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start command '{command}'.");

            // Read both redirected streams before awaiting the exit. This avoids deadlocks when
            // either pipe fills and also guarantees that no trailing output is lost after Exited.
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);

            return (process.ExitCode, outputTask.Result, errorTask.Result);
        }
    }
}
