namespace Greppa.Infrastructure.OpenAi;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5-mini";
    public int TimeoutSeconds { get; set; } = 60;
}
