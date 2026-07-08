using System.Text;
using Greppa.Api.Contracts;
using Greppa.Application.Interfaces;
using Greppa.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Greppa.Api.Controllers;

[ApiController]
[Route("api/scans")]
public sealed class ScansController(
    IJobStore jobStore,
    IUploadStore uploadStore,
    IScanQueue queue,
    IOptions<UploadOptions> options) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> StartScan(CancellationToken ct)
    {
        var limits = options.Value;
        IFormCollection form;
        try
        {
            form = await Request.ReadFormAsync(ct);
        }
        catch (BadHttpRequestException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = $"Upload exceeds the {limits.MaxTotalBytes / (1024 * 1024)} MB limit." });
        }
        catch (InvalidDataException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { error = $"Upload exceeds the {limits.MaxTotalBytes / (1024 * 1024)} MB limit." });
        }

        if (form.Files.Count == 0)
        {
            return BadRequest(new { error = "No files were uploaded." });
        }

        if (form.Files.Count > limits.MaxFileCount)
        {
            return BadRequest(new { error = $"Too many files (max {limits.MaxFileCount})." });
        }

        var files = new List<UploadedFile>();
        foreach (var formFile in form.Files)
        {
            var relativePath = (formFile.FileName ?? string.Empty).Replace('\\', '/').TrimStart('/');
            if (!IsScannable(relativePath, formFile.Length, limits))
            {
                continue;
            }

            using var reader = new StreamReader(formFile.OpenReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync(ct);
            if (content.Contains('\0'))
            {
                continue; // binary file
            }

            files.Add(new UploadedFile(relativePath, content));
        }

        if (files.Count == 0)
        {
            return BadRequest(new { error = "No scannable text files found in the upload." });
        }

        var job = jobStore.Create();
        await uploadStore.SaveAsync(job.Id, files, ct);
        await queue.EnqueueAsync(new ScanWorkItem(job.Id), ct);

        return AcceptedAtAction(nameof(GetScan), new { jobId = job.Id }, new { jobId = job.Id });
    }

    [HttpGet("{jobId:guid}")]
    public IActionResult GetScan(Guid jobId)
    {
        var job = jobStore.Get(jobId);
        return job is null ? NotFound(new { error = "Unknown or expired job." }) : Ok(job.ToResponse());
    }

    private static bool IsScannable(string relativePath, long length, UploadOptions limits)
    {
        if (relativePath.Length == 0 || length == 0 || length > limits.MaxFileBytes)
        {
            return false;
        }

        var segments = relativePath.Split('/');
        if (segments.Contains("..") || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        return !segments.Intersect(limits.IgnoredSegments, StringComparer.OrdinalIgnoreCase).Any()
            && !limits.IgnoredFileNames.Contains(segments[^1], StringComparer.OrdinalIgnoreCase);
    }
}
