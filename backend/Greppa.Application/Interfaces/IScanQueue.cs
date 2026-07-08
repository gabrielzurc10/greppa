namespace Greppa.Application.Interfaces;

public sealed record ScanWorkItem(Guid JobId);

public interface IScanQueue
{
    ValueTask EnqueueAsync(ScanWorkItem item, CancellationToken ct);
    IAsyncEnumerable<ScanWorkItem> DequeueAllAsync(CancellationToken ct);
}
