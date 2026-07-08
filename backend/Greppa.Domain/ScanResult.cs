namespace Greppa.Domain;

public sealed record ScanResult(
    IReadOnlyList<EnrichedFinding> Findings,
    IReadOnlyDictionary<Severity, int> SeveritySummary,
    int FilesScanned);
