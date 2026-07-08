using Greppa.Application.Interfaces;
using Greppa.Domain;

namespace Greppa.Infrastructure.Fakes;

/// <summary>
/// Deterministic scanner for local development without semgrep: reports one finding per
/// uploaded file plus a fixed spread of severities on the first file.
/// </summary>
public sealed class FakeScanner : IVulnerabilityScanner
{
    public async Task<IReadOnlyList<RawFinding>> ScanAsync(
        IReadOnlyList<UploadedFile> files, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct); // simulate scan latency

        var severities = Enum.GetValues<Severity>();
        return files
            .SelectMany((file, i) => severities
                .Take(i == 0 ? severities.Length : 1)
                .Select(severity => new RawFinding(
                    RuleId: $"fake.rules.demo-{severity.ToString().ToLowerInvariant()}",
                    FilePath: file.RelativePath,
                    StartLine: 1,
                    EndLine: 2,
                    Severity: severity,
                    Message: $"Fake {severity} finding for local development.",
                    Snippet: file.Content.Length > 200 ? file.Content[..200] : file.Content)))
            .ToList();
    }
}
