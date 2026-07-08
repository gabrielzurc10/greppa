using System.Text.Json;
using Greppa.Application.Interfaces;
using Greppa.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Greppa.Infrastructure.OpenAi;

/// <summary>Asks gpt-5-mini to explain a finding and propose a fix, constrained by a strict JSON schema.</summary>
public sealed class OpenAiFindingEnricher : IFindingEnricher
{
    private const int MaxAttempts = 2;

    private readonly ChatClient _chat;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiFindingEnricher> _logger;

    public OpenAiFindingEnricher(IOptions<OpenAiOptions> options, ILogger<OpenAiFindingEnricher> logger)
    {
        _options = options.Value;
        _logger = logger;
        _chat = new ChatClient(_options.Model, _options.ApiKey);
    }

    public async Task<EnrichedFinding> EnrichAsync(RawFinding finding, CancellationToken ct)
    {
        // Reasoning models (gpt-5-mini) reject temperature; leave all sampling params unset.
        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                EnrichmentSchema.Name, EnrichmentSchema.Json, jsonSchemaIsStrict: true),
        };

        List<ChatMessage> messages =
        [
            new SystemChatMessage(
                "You are an application security expert reviewing static-analysis findings. " +
                "Explain concretely why the flagged code is dangerous and give a practical fix. " +
                "Base your answer only on the provided finding and code."),
            new UserChatMessage(
                $"""
                Semgrep rule: {finding.RuleId}
                Severity: {finding.Severity}
                Semgrep message: {finding.Message}
                File: {finding.FilePath} (lines {finding.StartLine}-{finding.EndLine})

                Code around the finding:
                ```
                {finding.Snippet}
                ```
                """),
        ];

        for (var attempt = 1; ; attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            try
            {
                ChatCompletion completion = await _chat.CompleteChatAsync(messages, chatOptions, timeout.Token);
                var payload = JsonDocument.Parse(completion.Content[0].Text).RootElement;
                return new EnrichedFinding(
                    Guid.NewGuid(),
                    finding,
                    Reason: payload.GetProperty("reason").GetString() ?? finding.Message,
                    SuggestedFix: payload.GetProperty("suggestedFix").GetString() ?? string.Empty);
            }
            catch (Exception ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Enrichment attempt {Attempt} failed for {RuleId}; retrying",
                    attempt, finding.RuleId);
            }
        }
    }
}
