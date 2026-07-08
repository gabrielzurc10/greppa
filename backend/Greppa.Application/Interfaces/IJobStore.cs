using Greppa.Domain;

namespace Greppa.Application.Interfaces;

public interface IJobStore
{
    ScanJob Create();
    ScanJob? Get(Guid id);

    /// <summary>Applies <paramref name="mutate"/> atomically with respect to other updates of the same job.</summary>
    void Update(Guid id, Action<ScanJob> mutate);
}
