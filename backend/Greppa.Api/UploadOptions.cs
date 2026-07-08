namespace Greppa.Api;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    public long MaxTotalBytes { get; set; } = 50 * 1024 * 1024;
    public long MaxFileBytes { get; set; } = 1024 * 1024;
    public int MaxFileCount { get; set; } = 200;

    /// <summary>Path segments that mark a file as not worth scanning.</summary>
    public string[] IgnoredSegments { get; set; } = ["node_modules", ".git", ".angular", "dist", "bin", "obj"];

    public string[] IgnoredFileNames { get; set; } =
        ["package-lock.json", "yarn.lock", "pnpm-lock.yaml", "poetry.lock", "Cargo.lock", ".DS_Store"];
}
