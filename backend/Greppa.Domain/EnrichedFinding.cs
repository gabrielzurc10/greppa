namespace Greppa.Domain;

/// <summary>A finding augmented with an LLM-generated explanation and suggested fix.</summary>
public sealed record EnrichedFinding(
    Guid Id,
    RawFinding Finding,
    string Reason,
    string SuggestedFix);
