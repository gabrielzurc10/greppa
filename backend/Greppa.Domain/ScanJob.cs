namespace Greppa.Domain;

/// <summary>Mutable job aggregate; instances are updated through IJobStore only.</summary>
public sealed class ScanJob
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public int TotalFindings { get; set; }
    public int EnrichedCount { get; set; }
    public string? Error { get; set; }
    public ScanResult? Result { get; set; }
}
