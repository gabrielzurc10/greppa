using System.Text.Json;
using Greppa.Application.Interfaces;
using Greppa.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Greppa.Infrastructure.Semgrep;

/// <summary>
/// Scans uploads via the semgrep MCP server's scan tool, which takes absolute paths of
/// files already persisted to local disk.
/// </summary>
public sealed class SemgrepMcpScanner(
    SemgrepMcpClientFactory factory,
    IOptions<SemgrepOptions> options,
    ILogger<SemgrepMcpScanner> logger) : IVulnerabilityScanner
{
    public async Task<IReadOnlyList<RawFinding>> ScanAsync(
        IReadOnlyList<UploadedFile> files, CancellationToken ct)
    {
        var onDisk = files.Where(f => f.AbsolutePath is not null).ToList();
        if (onDisk.Count != files.Count)
        {
            throw new InvalidOperationException(
                "SemgrepMcpScanner requires files persisted to disk (AbsolutePath set).");
        }

        var client = await factory.GetClientAsync(ct);
        var findings = new List<RawFinding>();

        foreach (var batch in Batch(onDisk))
        {
            var result = await client.CallToolAsync(
                SemgrepMcpClientFactory.ScanToolName,
                new Dictionary<string, object?>
                {
                    ["code_files"] = batch
                        .Select(f => new Dictionary<string, object?> { ["path"] = f.AbsolutePath })
                        .ToList(),
                },
                cancellationToken: ct);

            findings.AddRange(SemgrepResultParser.Parse(GetPayload(result), batch));
        }

        logger.LogInformation("semgrep reported {Count} findings across {Files} files",
            findings.Count, files.Count);
        return findings;
    }

    /// <summary>
    /// Results are matched back to files by basename (the daemon scans renamed copies),
    /// so a batch must never contain two files with the same basename.
    /// </summary>
    private IEnumerable<IReadOnlyList<UploadedFile>> Batch(IReadOnlyList<UploadedFile> files)
    {
        var remaining = new List<UploadedFile>(files);
        while (remaining.Count > 0)
        {
            var batch = new List<UploadedFile>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in remaining)
            {
                if (batch.Count == options.Value.MaxFilesPerBatch)
                {
                    break;
                }

                if (names.Add(Path.GetFileName(file.RelativePath)))
                {
                    batch.Add(file);
                }
            }

            remaining.RemoveAll(batch.Contains);
            yield return batch;
        }
    }

    private static JsonElement GetPayload(CallToolResult result)
    {
        if (result.IsError == true)
        {
            var error = result.Content?.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "unknown error";
            throw new InvalidOperationException($"semgrep_scan failed: {error}");
        }

        if (result.StructuredContent is JsonElement structured
            && structured.ValueKind is JsonValueKind.Object)
        {
            return structured;
        }

        var text = result.Content?.OfType<TextContentBlock>().FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("semgrep_scan returned no content.");
        return JsonDocument.Parse(text).RootElement;
    }
}
