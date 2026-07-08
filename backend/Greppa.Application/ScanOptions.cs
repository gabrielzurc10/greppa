namespace Greppa.Application;

public sealed class ScanOptions
{
    public const string SectionName = "Scan";

    /// <summary>Findings beyond this cap keep the scanner's own message instead of an LLM explanation.</summary>
    public int MaxEnrichedFindings { get; set; } = 50;

    public int EnrichmentParallelism { get; set; } = 4;
}
