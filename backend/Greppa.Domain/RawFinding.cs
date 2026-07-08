namespace Greppa.Domain;

/// <summary>A vulnerability finding as reported by the scanner, before LLM enrichment.</summary>
public sealed record RawFinding(
    string RuleId,
    string FilePath,
    int StartLine,
    int EndLine,
    Severity Severity,
    string Message,
    string Snippet);
