# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Greppa.Domain/Greppa.Domain.csproj Greppa.Domain/
COPY Greppa.Application/Greppa.Application.csproj Greppa.Application/
COPY Greppa.Infrastructure/Greppa.Infrastructure.csproj Greppa.Infrastructure/
COPY Greppa.Api/Greppa.Api.csproj Greppa.Api/
RUN dotnet restore Greppa.Api/Greppa.Api.csproj
COPY . .
RUN dotnet publish Greppa.Api/Greppa.Api.csproj -c Release -o /app --no-restore

# --- runtime: ASP.NET 10 + semgrep (its MCP server ships in the binary: `semgrep mcp`) ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# setuptools<81: semgrep's opentelemetry dependency still imports pkg_resources.
RUN apt-get update \
 && apt-get install -y --no-install-recommends python3 python3-venv \
 && rm -rf /var/lib/apt/lists/* \
 && python3 -m venv /opt/semgrep \
 && /opt/semgrep/bin/pip install --no-cache-dir semgrep==1.168.0 "setuptools<81"

ENV PATH="/opt/semgrep/bin:${PATH}" \
    ASPNETCORE_URLS=http://+:8080 \
    SEMGREP_MCP_COMMAND=/opt/semgrep/bin/semgrep

WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Greppa.Api.dll"]
