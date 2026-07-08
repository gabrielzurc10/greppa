using Greppa.Api;
using Greppa.Api.Workers;
using Greppa.Application;
using Greppa.Application.Services;
using Greppa.Infrastructure;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ScanOptions>(builder.Configuration.GetSection(ScanOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));

var maxTotalBytes = builder.Configuration
    .GetSection(UploadOptions.SectionName).Get<UploadOptions>()?.MaxTotalBytes
    ?? new UploadOptions().MaxTotalBytes;
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = maxTotalBytes);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxTotalBytes);

builder.Services.AddGreppaInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ScanOrchestrator>();
builder.Services.AddHostedService<ScanWorker>();
builder.Services.AddControllers();

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
if (!string.IsNullOrEmpty(allowedOrigin))
{
    builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
        p.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.ContentLength > maxTotalBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(
            new { error = $"Upload exceeds the {maxTotalBytes / (1024 * 1024)} MB limit." });
        return;
    }

    await next(context);
});

if (!string.IsNullOrEmpty(allowedOrigin))
{
    app.UseCors();
}

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();
