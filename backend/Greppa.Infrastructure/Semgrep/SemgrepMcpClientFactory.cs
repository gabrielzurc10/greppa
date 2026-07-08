using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Greppa.Infrastructure.Semgrep;

/// <summary>
/// Owns a single `semgrep mcp` stdio subprocess for the app's lifetime: startup and
/// first-run rule downloads are slow, so the client is created lazily and reused.
/// </summary>
public sealed class SemgrepMcpClientFactory(
    IOptions<SemgrepOptions> options,
    ILoggerFactory loggerFactory) : IAsyncDisposable
{
    public const string ScanToolName = "semgrep_scan";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private McpClient? _client;

    public async Task<McpClient> GetClientAsync(CancellationToken ct)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_client is null)
            {
                var logger = loggerFactory.CreateLogger<SemgrepMcpClientFactory>();
                logger.LogInformation("Starting semgrep MCP server: {Command} {Args}",
                    options.Value.McpCommand, string.Join(' ', options.Value.McpArguments));

                var env = new Dictionary<string, string?>();
                if (!string.IsNullOrEmpty(options.Value.AppToken))
                {
                    env["SEMGREP_APP_TOKEN"] = options.Value.AppToken;
                }

                var transport = new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "semgrep",
                    Command = options.Value.McpCommand,
                    Arguments = options.Value.McpArguments,
                    EnvironmentVariables = env,
                }, loggerFactory);

                var client = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: ct);
                var tools = await client.ListToolsAsync(cancellationToken: ct);
                if (tools.All(t => t.Name != ScanToolName))
                {
                    throw new InvalidOperationException(
                        $"semgrep MCP server does not expose '{ScanToolName}'. Available: {string.Join(", ", tools.Select(t => t.Name))}");
                }

                logger.LogInformation("semgrep MCP server ready");
                _client = client;
            }

            return _client;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        _initLock.Dispose();
    }
}
