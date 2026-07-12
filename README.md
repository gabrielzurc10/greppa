# Greppa

Greppa is a web app that scans source code for security vulnerabilities and explains the results in plain language. You upload a file or a folder, Greppa runs [Semgrep](https://semgrep.dev) against it, and an LLM enriches every finding with an explanation of why it matters and a suggested fix. The results are presented as a report with a severity breakdown and per-finding details (file, line, severity, explanation, fix).

## The problem

Static analysis tools are powerful but unapproachable. Raw scanner output is terse, assumes security expertise, and rarely tells you what to actually do about a finding. Developers who are not security specialists either ignore the results or spend time researching each rule by hand.

Greppa closes that gap by pairing a proven scanner with an LLM:

- Semgrep provides accurate, rule-based detection.
- GPT-5-mini translates each raw finding into a human-readable explanation and a concrete suggested fix.
- A simple upload-and-wait UI means no local tooling, configuration, or CLI knowledge is required.

## How it works

1. The user uploads files through the Angular frontend.
2. The API accepts the upload, immediately returns a job id, and queues the scan.
3. A background worker runs Semgrep through its MCP server (via the Model Context Protocol, using the official C# SDK over stdio).
4. Each finding is enriched by GPT-5-mini using structured JSON output, with a graceful fallback to Semgrep's own message if enrichment fails.
5. The frontend polls the job until it completes, then renders the report.

Scans are asynchronous by design: cloud ingress timeouts are shorter than a large scan plus LLM calls, so the job-and-poll model is a necessity, not a preference.

## Architecture

```
Angular SPA (Azure Static Web Apps)
      |  upload / poll
      v
ASP.NET Core API (Azure Container Apps, scale to zero)
      |-- Semgrep MCP server (stdio subprocess)
      |-- OpenAI API (gpt-5-mini, structured outputs)
```

**Frontend.** An Angular 21 single-page app using standalone components and signals, with a Chart.js severity chart. Hosted on Azure Static Web Apps.

**Backend.** A .NET 10 Web API structured in four layers following SOLID principles: `Greppa.Domain` (entities), `Greppa.Application` (interfaces and the scan orchestrator), `Greppa.Infrastructure` (Semgrep MCP client, OpenAI client, in-memory job store, channel-based queue), and `Greppa.Api` (composition root). Everything is behind interfaces and constructor-injected, so the scanner and enricher can be swapped for offline fakes with a single configuration flag.

**Infrastructure.** Terraform provisions all Azure resources: a Container App running the backend image from Azure Container Registry, a Static Web App for the frontend, and Log Analytics. The Container App scales to zero when idle to keep costs near zero; a single replica is used because job state is in memory. GitHub Actions deploys on push to main using OIDC (no stored cloud credentials).

## Repository layout

| Path | What it is |
|---|---|
| `backend/` | .NET 10 Web API (four-project solution plus Dockerfile) |
| `frontend/` | Angular 21 SPA |
| `terraform/` | Azure infrastructure as code |
| `scripts/` | One-time bootstrap scripts (Terraform state, GitHub OIDC) |
| `.github/workflows/` | Deploy (push to main) and destroy (manual) workflows |

## Run locally

Requires Docker, Node, and a `.env` at the repo root with `OPENAI_API_KEY` and `SEMGREP_APP_TOKEN`.

```bash
# real pipeline: semgrep + gpt-5-mini in a container
docker compose up --build backend

# frontend with /api proxied to the backend
cd frontend && npm install && npm start
```

For a fast loop without Docker or API keys, run `dotnet run --project backend/Greppa.Api`; the Development configuration uses fake scanner and enricher implementations.

## Deploy

Push to main. The GitHub Actions workflow applies Terraform, builds and pushes the backend image, deploys the Container App, and publishes the frontend to Static Web Apps. A manual destroy workflow tears everything down between demos.
