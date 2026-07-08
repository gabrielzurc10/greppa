namespace Greppa.Infrastructure.OpenAi;

/// <summary>Strict JSON schema forcing the model to return exactly { reason, suggestedFix }.</summary>
public static class EnrichmentSchema
{
    public const string Name = "finding_enrichment";

    public static readonly BinaryData Json = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "reason": {
              "type": "string",
              "description": "Why this code is vulnerable and what an attacker could do, in 2-4 plain sentences."
            },
            "suggestedFix": {
              "type": "string",
              "description": "How to fix it, including a corrected code snippet in a markdown code block when applicable."
            }
          },
          "required": ["reason", "suggestedFix"],
          "additionalProperties": false
        }
        """);
}
