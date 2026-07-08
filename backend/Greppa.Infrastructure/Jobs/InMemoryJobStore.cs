using System.Collections.Concurrent;
using Greppa.Application.Interfaces;
using Greppa.Domain;

namespace Greppa.Infrastructure.Jobs;

/// <summary>
/// Single-replica job store. Jobs expire after <see cref="Ttl"/>; expired entries are
/// swept opportunistically on access.
/// </summary>
public sealed class InMemoryJobStore : IJobStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    private readonly ConcurrentDictionary<Guid, ScanJob> _jobs = new();

    public ScanJob Create()
    {
        Sweep();
        var job = new ScanJob { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        _jobs[job.Id] = job;
        return job;
    }

    public ScanJob? Get(Guid id) =>
        _jobs.TryGetValue(id, out var job) && !IsExpired(job) ? job : null;

    public void Update(Guid id, Action<ScanJob> mutate)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            return;
        }

        lock (job)
        {
            mutate(job);
        }
    }

    private static bool IsExpired(ScanJob job) => DateTimeOffset.UtcNow - job.CreatedAt > Ttl;

    private void Sweep()
    {
        foreach (var (id, job) in _jobs)
        {
            if (IsExpired(job))
            {
                _jobs.TryRemove(id, out _);
            }
        }
    }
}
