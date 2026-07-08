using Greppa.Application.Interfaces;
using Greppa.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greppa.Application.Services;

/// <summary>Runs a scan job end to end: scan → enrich (bounded parallel) → aggregate.</summary>
public sealed class ScanOrchestrator(
    IVulnerabilityScanner scanner,
    IFindingEnricher enricher,
    IJobStore jobStore,
    IUploadStore uploadStore,
    IOptions<ScanOptions> options,
    ILogger<ScanOrchestrator> logger)
{
    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        try
        {
            jobStore.Update(jobId, j => j.Status = JobStatus.Scanning);
            var files = await uploadStore.LoadAsync(jobId, ct);
            var findings = await scanner.ScanAsync(files, ct);

            jobStore.Update(jobId, j =>
            {
                j.Status = JobStatus.Enriching;
                j.TotalFindings = findings.Count;
            });

            var enriched = await EnrichAsync(jobId, findings, ct);

            var summary = Enum.GetValues<Severity>()
                .ToDictionary(s => s, s => enriched.Count(f => f.Finding.Severity == s));

            jobStore.Update(jobId, j =>
            {
                j.Result = new ScanResult(enriched, summary, files.Count);
                j.Status = JobStatus.Completed;
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scan job {JobId} failed", jobId);
            jobStore.Update(jobId, j =>
            {
                j.Status = JobStatus.Failed;
                j.Error = "The scan failed unexpectedly. Please try again.";
            });
        }
        finally
        {
            uploadStore.Delete(jobId);
        }
    }

    private async Task<IReadOnlyList<EnrichedFinding>> EnrichAsync(
        Guid jobId, IReadOnlyList<RawFinding> findings, CancellationToken ct)
    {
        var ordered = findings.OrderByDescending(f => f.Severity).ToList();
        var toEnrich = ordered.Take(options.Value.MaxEnrichedFindings).ToList();
        var overflow = ordered.Skip(options.Value.MaxEnrichedFindings);

        using var gate = new SemaphoreSlim(options.Value.EnrichmentParallelism);
        var tasks = toEnrich.Select(async finding =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var result = await EnrichOneAsync(finding, ct);
                jobStore.Update(jobId, j => j.EnrichedCount++);
                return result;
            }
            finally
            {
                gate.Release();
            }
        });

        var enriched = await Task.WhenAll(tasks);
        return [.. enriched, .. overflow.Select(Fallback)];
    }

    private async Task<EnrichedFinding> EnrichOneAsync(RawFinding finding, CancellationToken ct)
    {
        try
        {
            return await enricher.EnrichAsync(finding, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Enrichment failed for {RuleId} in {File}; using scanner message",
                finding.RuleId, finding.FilePath);
            return Fallback(finding);
        }
    }

    private static EnrichedFinding Fallback(RawFinding finding) =>
        new(Guid.NewGuid(), finding, finding.Message, string.Empty);
}
