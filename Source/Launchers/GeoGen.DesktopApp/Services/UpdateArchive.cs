using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;

namespace GeoGen.DesktopApp.Services;

public static class UpdateArchive
{
    private const long MaximumExtractedBytes = 2_147_483_648;

    public static string ApplicationDirectory(string directory, string rid) => rid.StartsWith("osx-", StringComparison.Ordinal)
        ? Path.Combine(directory, "Planar Geometry Studio.app", "Contents", "MacOS")
        : Path.Combine(directory, "PlanarGeometryStudio");

    public static string ExecutableName(string rid) => rid.StartsWith("win-", StringComparison.Ordinal)
        ? "PlanarGeometryStudio.exe" : "PlanarGeometryStudio";

    public static Dictionary<string, string> Extract(string archive, string destination, string version, string rid,
        CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
            throw new IOException("Update extraction requires an empty directory.");
        Directory.CreateDirectory(destination);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        string GetPath(string name, long length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parts = name.TrimEnd('/').Split('/');
            var root = rid.StartsWith("osx-", StringComparison.Ordinal) ? "Planar Geometry Studio.app" : "PlanarGeometryStudio";
            if (parts.Length == 0 || parts[0] != root || parts.Any(part => part is "" or "." or ".." || part.Contains('\\') || part.Contains(':')) ||
                !paths.Add(name.TrimEnd('/')) || paths.Count > 10_000 || length < 0 || length > MaximumExtractedBytes - total)
                throw new InvalidDataException("The update archive has an unsafe path or exceeds extraction limits.");
            total += length;
            return Path.Combine(destination, Path.Combine(parts));
        }
        using var input = File.OpenRead(archive);
        if (rid.StartsWith("linux-", StringComparison.Ordinal))
        {
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new TarReader(gzip);
            TarEntry? entry;
            while ((entry = reader.GetNextEntry()) is not null)
            {
                if (entry.EntryType is not (TarEntryType.Directory or TarEntryType.RegularFile or TarEntryType.V7RegularFile))
                    throw new InvalidDataException("Links and special files are not allowed in updates.");
                var path = GetPath(entry.Name, entry.Length);
                if (entry.EntryType == TarEntryType.Directory) Directory.CreateDirectory(path);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    if (entry.DataStream is { } data) CopyBounded(data, output, entry.Length, cancellationToken);
                    output.Close();
                    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, entry.Mode & (UnixFileMode)511);
                }
            }
        }
        else
        {
            using var zip = new ZipArchive(input, ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                var mode = (entry.ExternalAttributes >> 16) & 0xffff;
                var type = mode & 0xf000;
                if (type is not (0 or 0x8000 or 0x4000))
                    throw new InvalidDataException("Links and special files are not allowed in updates.");
                var path = GetPath(entry.FullName, entry.Length);
                if (entry.FullName.EndsWith('/')) Directory.CreateDirectory(path);
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    using var source = entry.Open();
                    using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    CopyBounded(source, output, entry.Length, cancellationToken);
                    output.Close();
                    if (!OperatingSystem.IsWindows() && mode != 0) File.SetUnixFileMode(path, (UnixFileMode)(mode & 511));
                }
            }
        }
        var app = ApplicationDirectory(destination, rid);
        if (File.ReadAllText(Path.Combine(app, "studio-version.txt")).Trim().Trim('\uFEFF') != version)
            throw new InvalidDataException("The update contains a different application version.");
        foreach (var path in new[] { ExecutableName(rid),
            Path.Combine("tools", "engine", rid.StartsWith("win-", StringComparison.Ordinal) ? "GeoGen.exe" : "GeoGen"),
            Path.Combine("tools", "drawer", rid.StartsWith("win-", StringComparison.Ordinal) ? "GeoGen.DrawingLauncher.exe" : "GeoGen.DrawingLauncher"), "TERMS.md" })
            if (!File.Exists(Path.Combine(app, path)) || new FileInfo(Path.Combine(app, path)).Length == 0)
                throw new InvalidDataException("The update is missing application files.");
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = File.OpenRead(path);
            hashes.Add(Path.GetRelativePath(destination, path), Convert.ToHexString(SHA256.HashData(stream)));
        }
        return hashes;
    }
    private static void CopyBounded(Stream source, Stream output, long expected, CancellationToken cancellationToken)
    {
        var buffer = new byte[131_072];
        long total = 0;
        int count;
        while ((count = source.Read(buffer)) != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += count;
            if (total > expected) throw new InvalidDataException("An update file exceeded its declared size.");
            output.Write(buffer, 0, count);
        }
        if (total != expected) throw new InvalidDataException("An update file was incomplete.");
    }

}
