using System.Threading.Channels;
using Greppa.Application.Interfaces;

namespace Greppa.Infrastructure.Jobs;

public sealed class ChannelScanQueue : IScanQueue
{
    private readonly Channel<ScanWorkItem> _channel =
        Channel.CreateUnbounded<ScanWorkItem>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(ScanWorkItem item, CancellationToken ct) =>
        _channel.Writer.WriteAsync(item, ct);

    public IAsyncEnumerable<ScanWorkItem> DequeueAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
