using Greppa.Domain;

namespace Greppa.Application.Interfaces;

/// <summary>Persists uploaded files for the lifetime of a scan job (temp dir on disk in production).</summary>
public interface IUploadStore
{
    Task SaveAsync(Guid jobId, IReadOnlyList<UploadedFile> files, CancellationToken ct);
    Task<IReadOnlyList<UploadedFile>> LoadAsync(Guid jobId, CancellationToken ct);
    void Delete(Guid jobId);
}
