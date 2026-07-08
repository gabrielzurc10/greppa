namespace Greppa.Infrastructure.Semgrep;

public sealed class SemgrepOptions
{
    public const string SectionName = "Semgrep";

    /// <summary>Executable that hosts the semgrep MCP server (the semgrep binary itself).</summary>
    public string McpCommand { get; set; } = "semgrep";

    /// <summary>Arguments that start the MCP server in stdio mode.</summary>
    public string[] McpArguments { get; set; } = ["mcp"];

    /// <summary>Optional semgrep.dev token, forwarded to the MCP server process.</summary>
    public string? AppToken { get; set; }

    public int MaxFilesPerBatch { get; set; } = 30;
}
