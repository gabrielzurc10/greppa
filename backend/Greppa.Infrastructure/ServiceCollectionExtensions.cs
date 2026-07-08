using Greppa.Application.Interfaces;
using Greppa.Infrastructure.Fakes;
using Greppa.Infrastructure.Jobs;
using Greppa.Infrastructure.OpenAi;
using Greppa.Infrastructure.Semgrep;
using Greppa.Infrastructure.Uploads;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Greppa.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Wires all infrastructure implementations behind their Application interfaces.</summary>
    public static IServiceCollection AddGreppaInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJobStore, InMemoryJobStore>();
        services.AddSingleton<IScanQueue, ChannelScanQueue>();
        services.AddSingleton<IUploadStore, FileSystemUploadStore>();

        if (configuration.GetValue("Scanner:UseFake", false))
        {
            services.AddSingleton<IVulnerabilityScanner, FakeScanner>();
        }
        else
        {
            services.Configure<SemgrepOptions>(configuration.GetSection(SemgrepOptions.SectionName));
            services.PostConfigure<SemgrepOptions>(o =>
            {
                // Container/hosting envs configure via flat env vars rather than the Semgrep section.
                o.McpCommand = configuration["SEMGREP_MCP_COMMAND"] ?? o.McpCommand;
                o.AppToken ??= configuration["SEMGREP_APP_TOKEN"];
            });
            services.AddSingleton<SemgrepMcpClientFactory>();
            services.AddSingleton<IVulnerabilityScanner, SemgrepMcpScanner>();
        }

        if (configuration.GetValue("Enricher:UseFake", false))
        {
            services.AddSingleton<IFindingEnricher, FakeEnricher>();
        }
        else
        {
            services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
            services.PostConfigure<OpenAiOptions>(o =>
            {
                if (string.IsNullOrEmpty(o.ApiKey))
                {
                    o.ApiKey = configuration["OPENAI_API_KEY"]
                        ?? throw new InvalidOperationException(
                            "OPENAI_API_KEY is not set; set it or use Enricher:UseFake=true.");
                }
            });
            services.AddSingleton<IFindingEnricher, OpenAiFindingEnricher>();
        }

        return services;
    }
}
