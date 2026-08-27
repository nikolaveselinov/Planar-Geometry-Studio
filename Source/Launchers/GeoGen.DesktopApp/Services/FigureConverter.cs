using GeoGen.DesktopApp.Models;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace GeoGen.DesktopApp.Services;

public sealed partial class FigureConverter
{
    private readonly ProcessRunner _processRunner;
    private readonly Action<string> _onOutput;

    public FigureConverter(ProcessRunner processRunner, Action<string> onOutput)
    {
        _processRunner = processRunner;
        _onOutput = onOutput;
    }

    public async Task<FigureConversionResult> ConvertAsync(
        string figureDataDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);

        var sourceFiles = Directory
            .EnumerateFiles(figureDataDirectory)
            .Select(path => new { Path = path, Match = FigureFileRegex().Match(Path.GetFileName(path)) })
            .Where(item => item.Match.Success)
            .OrderBy(item => int.Parse(item.Match.Groups[1].Value))
            .ToArray();

        if (sourceFiles.Length == 0)
            return new FigureConversionResult(0, 0, 0, Array.Empty<string>());

        var converted = 0;
        var fallbacks = 0;
        var failed = new List<string>();

        foreach (var item in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var number = int.Parse(item.Match.Groups[1].Value);
            var pdfPath = Path.Combine(destinationDirectory, $"figure-{number:D4}.pdf");
            _onOutput($"Converting figure {number}... ");

            if (await TryConvertAsync(item.Path, pdfPath, figureDataDirectory, cancellationToken))
            {
                converted++;
                _onOutput("done" + Environment.NewLine);
                continue;
            }

            var epsPath = Path.Combine(destinationDirectory, $"figure-{number:D4}.eps");
            try
            {
                File.Copy(item.Path, epsPath, overwrite: true);
                fallbacks++;
                _onOutput("PDF converter unavailable; saved EPS" + Environment.NewLine);
            }
            catch
            {
                failed.Add(Path.GetFileName(item.Path));
                _onOutput("failed" + Environment.NewLine);
            }
        }

        return new FigureConversionResult(sourceFiles.Length, converted, fallbacks, failed);
    }

    private async Task<bool> TryConvertAsync(
        string epsPath,
        string pdfPath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var attempts = new List<(string Command, string[] Arguments)>
        {
            ("epstopdf", new[] { $"--outfile={pdfPath}", epsPath }),
            ("miktex-epstopdf", new[] { $"--outfile={pdfPath}", epsPath })
        };

        var ghostscript = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "gswin64c" : "gs";
        attempts.Add((ghostscript, new[]
        {
            "-sDEVICE=pdfwrite",
            "-dNOPAUSE",
            "-dBATCH",
            "-dEPSCrop",
            $"-sOutputFile={pdfPath}",
            epsPath
        }));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            attempts.Add(("gswin32c", new[]
            {
                "-sDEVICE=pdfwrite",
                "-dNOPAUSE",
                "-dBATCH",
                "-dEPSCrop",
                $"-sOutputFile={pdfPath}",
                epsPath
            }));
        }

        foreach (var attempt in attempts)
        {
            try
            {
                var result = await _processRunner.RunAsync(
                    attempt.Command,
                    attempt.Arguments,
                    workingDirectory,
                    standardInputLines: null,
                    cancellationToken);

                if (result.ExitCode == 0 && File.Exists(pdfPath))
                    return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Try the next converter.
            }
        }

        return false;
    }

    [GeneratedRegex(@"^figures\.(\d+)(?:\.eps)?$", RegexOptions.IgnoreCase)]
    private static partial Regex FigureFileRegex();
}
