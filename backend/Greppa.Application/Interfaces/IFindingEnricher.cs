using Greppa.Domain;

namespace Greppa.Application.Interfaces;

/// <summary>Generates a human explanation and suggested fix for a raw finding (LLM-backed).</summary>
public interface IFindingEnricher
{
    Task<EnrichedFinding> EnrichAsync(RawFinding finding, CancellationToken ct);
}
