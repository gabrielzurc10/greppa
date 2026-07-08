namespace Greppa.Api.Contracts;

public sealed record ScanJobResponse(
    Guid JobId,
    string Status,
    ScanProgressDto Progress,
    string? Error,
    ScanResultDto? Result);

public sealed record ScanProgressDto(int TotalFindings, int Enriched);

public sealed record ScanResultDto(ScanSummaryDto Summary, IReadOnlyList<FindingDto> Findings);

public sealed record ScanSummaryDto(
    int Critical, int High, int Medium, int Low, int Info, int Total, int FilesScanned);

public sealed record FindingDto(
    Guid Id,
    string Severity,
    string FilePath,
    int Line,
    int EndLine,
    string RuleId,
    string Message,
    string Reason,
    string SuggestedFix);
