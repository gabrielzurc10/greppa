using Greppa.Application.Interfaces;
using Greppa.Domain;

namespace Greppa.Infrastructure.Uploads;

/// <summary>Stores each job's files under a per-job temp directory for the duration of the scan.</summary>
public sealed class FileSystemUploadStore : IUploadStore
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "greppa");

    public async Task SaveAsync(Guid jobId, IReadOnlyList<UploadedFile> files, CancellationToken ct)
    {
        var jobDir = JobDir(jobId);
        foreach (var file in files)
        {
            var target = SafePath(jobDir, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllTextAsync(target, file.Content, ct);
        }
    }

    public async Task<IReadOnlyList<UploadedFile>> LoadAsync(Guid jobId, CancellationToken ct)
    {
        var jobDir = JobDir(jobId);
        var files = new List<UploadedFile>();
        foreach (var path in Directory.EnumerateFiles(jobDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(jobDir, path).Replace('\\', '/');
            files.Add(new UploadedFile(relative, await File.ReadAllTextAsync(path, ct), path));
        }

        return files;
    }

    public void Delete(Guid jobId)
    {
        var jobDir = JobDir(jobId);
        if (Directory.Exists(jobDir))
        {
            Directory.Delete(jobDir, recursive: true);
        }
    }

    private string JobDir(Guid jobId) => Path.Combine(_root, jobId.ToString("N"));

    /// <summary>Resolves a client-supplied relative path, rejecting traversal outside the job dir.</summary>
    private static string SafePath(string jobDir, string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(jobDir, relativePath));
        if (!full.StartsWith(Path.GetFullPath(jobDir) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Rejected path outside upload root: {relativePath}");
        }

        return full;
    }
}
