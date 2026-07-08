using System.Text.Json;
using System.Text.RegularExpressions;
using Greppa.Domain;

namespace Greppa.Infrastructure.Semgrep;

/// <summary>Maps semgrep JSON output (results[] with check_id/path/start/end/extra) to domain findings.</summary>
public static class SemgrepResultParser
{
    private const int SnippetContextLines = 10;

    public static IReadOnlyList<RawFinding> Parse(JsonElement root, IReadOnlyList<UploadedFile> files)
    {
        if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var findings = new List<RawFinding>();
        foreach (var result in results.EnumerateArray())
        {
            var reportedPath = GetString(result, "path") ?? "unknown";
            var file = Resolve(reportedPath, files);
            var startLine = GetLine(result, "start");
            var endLine = Math.Max(GetLine(result, "end"), startLine);
            var extra = result.TryGetProperty("extra", out var e) ? e : default;

            findings.Add(new RawFinding(
                RuleId: GetString(result, "check_id") ?? "unknown-rule",
                FilePath: file?.RelativePath ?? reportedPath,
                StartLine: startLine,
                EndLine: endLine,
                Severity: MapSeverity(extra),
                Message: extra.ValueKind == JsonValueKind.Object ? GetString(extra, "message") ?? string.Empty : string.Empty,
                Snippet: ExtractSnippet(file?.Content, startLine, endLine)));
        }

        return findings;
    }

    // The daemon copies each file to /tmp/<basename><counter>-<hash>.<ext> before scanning.
    private static readonly Regex CopySuffix = new(@"\d+-[0-9a-f]{4,}\.[^.]+$", RegexOptions.Compiled);

    /// <summary>
    /// The semgrep mcp daemon scans a renamed copy of each file, so results are matched
    /// back to the upload by exact path or by the copy's original basename (unique per batch).
    /// </summary>
    private static UploadedFile? Resolve(string reportedPath, IReadOnlyList<UploadedFile> files)
    {
        var exact = files.FirstOrDefault(f => f.AbsolutePath == reportedPath);
        if (exact is not null)
        {
            return exact;
        }

        var originalName = CopySuffix.Replace(Path.GetFileName(reportedPath), string.Empty);
        return files.FirstOrDefault(f => Path.GetFileName(f.RelativePath) == originalName);
    }

    private static Severity MapSeverity(JsonElement extra)
    {
        if (extra.ValueKind != JsonValueKind.Object)
        {
            return Severity.Info;
        }

        var severity = (GetString(extra, "severity") ?? "INFO").ToUpperInvariant();
        var mapped = severity switch
        {
            "ERROR" or "HIGH" => Severity.High,
            "WARNING" or "MEDIUM" => Severity.Medium,
            "INFO" or "LOW" => Severity.Low,
            "CRITICAL" => Severity.Critical,
            _ => Severity.Info,
        };

        // Promote high-severity rules that semgrep's metadata marks as high impact AND likelihood.
        if (mapped == Severity.High
            && extra.TryGetProperty("metadata", out var meta)
            && meta.ValueKind == JsonValueKind.Object
            && string.Equals(GetString(meta, "impact"), "HIGH", StringComparison.OrdinalIgnoreCase)
            && string.Equals(GetString(meta, "likelihood"), "HIGH", StringComparison.OrdinalIgnoreCase))
        {
            return Severity.Critical;
        }

        return mapped;
    }

    private static string ExtractSnippet(string? content, int startLine, int endLine)
    {
        if (content is null)
        {
            return string.Empty;
        }

        var lines = content.Split('\n');
        var from = Math.Max(0, startLine - 1 - SnippetContextLines);
        var to = Math.Min(lines.Length, endLine + SnippetContextLines);
        return string.Join('\n', lines[from..to]);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int GetLine(JsonElement result, string position) =>
        result.TryGetProperty(position, out var pos)
            && pos.ValueKind == JsonValueKind.Object
            && pos.TryGetProperty("line", out var line)
            && line.ValueKind == JsonValueKind.Number
            ? line.GetInt32()
            : 1;
}
