using Greppa.Application.Interfaces;
using Greppa.Application.Services;

namespace Greppa.Api.Workers;

/// <summary>Drains the scan queue and runs jobs sequentially (single replica, semgrep is memory-hungry).</summary>
public sealed class ScanWorker(
    IScanQueue queue,
    ScanOrchestrator orchestrator,
    ILogger<ScanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.DequeueAllAsync(stoppingToken))
        {
            logger.LogInformation("Starting scan job {JobId}", item.JobId);
            await orchestrator.RunAsync(item.JobId, stoppingToken);
            logger.LogInformation("Finished scan job {JobId}", item.JobId);
        }
    }
}
