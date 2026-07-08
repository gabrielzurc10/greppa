using Greppa.Application.Interfaces;
using Greppa.Domain;

namespace Greppa.Infrastructure.Fakes;

/// <summary>Offline replacement for the LLM enricher; echoes canned advice.</summary>
public sealed class FakeEnricher : IFindingEnricher
{
    public async Task<EnrichedFinding> EnrichAsync(RawFinding finding, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(300), ct);
        return new EnrichedFinding(
            Guid.NewGuid(),
            finding,
            Reason: $"[fake] {finding.Message} This pattern is flagged by rule {finding.RuleId}.",
            SuggestedFix: "[fake] Replace the flagged code with a safe equivalent.");
    }
}
