using Greppa.Domain;

namespace Greppa.Api.Contracts;

public static class ScanJobMapper
{
    public static ScanJobResponse ToResponse(this ScanJob job) => new(
        job.Id,
        job.Status.ToString().ToLowerInvariant(),
        new ScanProgressDto(job.TotalFindings, job.EnrichedCount),
        job.Error,
        job.Result?.ToDto());

    private static ScanResultDto ToDto(this ScanResult result) => new(
        new ScanSummaryDto(
            Critical: result.SeveritySummary.GetValueOrDefault(Severity.Critical),
            High: result.SeveritySummary.GetValueOrDefault(Severity.High),
            Medium: result.SeveritySummary.GetValueOrDefault(Severity.Medium),
            Low: result.SeveritySummary.GetValueOrDefault(Severity.Low),
            Info: result.SeveritySummary.GetValueOrDefault(Severity.Info),
            Total: result.Findings.Count,
            FilesScanned: result.FilesScanned),
        result.Findings.Select(f => new FindingDto(
            f.Id,
            f.Finding.Severity.ToString().ToLowerInvariant(),
            f.Finding.FilePath,
            f.Finding.StartLine,
            f.Finding.EndLine,
            f.Finding.RuleId,
            f.Finding.Message,
            f.Reason,
            f.SuggestedFix)).ToList());
}
