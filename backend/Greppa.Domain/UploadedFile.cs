namespace Greppa.Domain;

/// <summary>
/// A single uploaded source file, identified by its path relative to the upload root.
/// AbsolutePath is set once the file has been persisted to local disk for scanning.
/// </summary>
public sealed record UploadedFile(string RelativePath, string Content, string? AbsolutePath = null);
