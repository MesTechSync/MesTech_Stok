# ─────────────────────────────────────────────────────────────
# MesTech Blazor — Multi-stage Docker build (Production)
# Target: Coolify / Docker Compose deployment on port 3200
# Base: .NET 9.0 SDK Alpine (build) + ASP.NET 9.0 Runtime Alpine (run)
#
# Build context: mono-repo root (e.g. docker build -f src/MesTech.Blazor/Dockerfile ../..)
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore (layer-cached) ──
# Pin: dotnet/sdk:9.0-alpine@sha256:5e2228a03bcb9b75b9078f7a2379c2c82639422b785601ff271f744745ac2f71
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine@sha256:5e2228a03bcb9b75b9078f7a2379c2c82639422b785601ff271f744745ac2f71 AS restore
WORKDIR /src

# Copy solution, global.json and build props first for optimal layer caching
COPY MesTech_Stok/MesTechStok/global.json ./global.json
COPY MesTech_Stok/MesTechStok/MesTechStok.sln ./MesTechStok.sln
COPY MesTech_Stok/MesTechStok/Directory.Build.props ./Directory.Build.props
COPY MesTech_Stok/MesTechStok/Directory.Packages.props ./Directory.Packages.props

# Copy all project files for restore (csproj-only layer — changes rarely)
COPY MesTech_Stok/MesTechStok/src/MesTech.Domain/MesTech.Domain.csproj ./src/MesTech.Domain/
COPY MesTech_Stok/MesTechStok/src/MesTech.Application/MesTech.Application.csproj ./src/MesTech.Application/
COPY MesTech_Stok/MesTechStok/src/MesTech.Infrastructure/MesTech.Infrastructure.csproj ./src/MesTech.Infrastructure/
COPY MesTech_Stok/MesTechStok/src/MesTech.WebApi/MesTech.WebApi.csproj ./src/MesTech.WebApi/
COPY MesTech_Stok/MesTechStok/src/MesTechStok.Core/MesTechStok.Core.csproj ./src/MesTechStok.Core/
COPY MesTech_Stok/MesTechStok/src/MesTechStok.Desktop/MesTechStok.Desktop.csproj ./src/MesTechStok.Desktop/
COPY MesTech_Stok/MesTechStok/src/MesTech.Blazor/MesTech.Blazor.csproj ./src/MesTech.Blazor/
COPY MesTech_Stok/MesTechStok/src/MesTech.Avalonia/MesTech.Avalonia.csproj ./src/MesTech.Avalonia/

# Restore only Blazor project (pulls transitive deps via solution graph)
RUN dotnet restore src/MesTech.Blazor/MesTech.Blazor.csproj \
    --runtime linux-musl-x64

# ── Stage 2: Build + Publish ──
FROM restore AS publish
# Copy full source (changes frequently — this layer rebuilds on code change)
COPY MesTech_Stok/MesTechStok/src/ ./src/

RUN dotnet publish src/MesTech.Blazor/MesTech.Blazor.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --runtime linux-musl-x64 \
    --self-contained false \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# ── Stage 3: Runtime (minimal attack surface) ──
# Pin: dotnet/aspnet:9.0-alpine@sha256:86883194752e8cb9c3914edea40961cc179da6804fcfe814f18f89ed6515d8cf
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine@sha256:86883194752e8cb9c3914edea40961cc179da6804fcfe814f18f89ed6515d8cf AS runtime

# Security: non-root user
RUN addgroup -S mestech && adduser -S mestech -G mestech \
    && mkdir -p /app \
    && chown -R mestech:mestech /app

WORKDIR /app

# Copy published output from build stage (owned by mestech)
COPY --from=publish --chown=mestech:mestech /app/publish .

# Switch to non-root user
USER mestech

# Expose Blazor port
EXPOSE 3200

# ASP.NET Core configuration
ENV ASPNETCORE_URLS=http://+:3200 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

# Labels for container metadata
LABEL org.opencontainers.image.source="https://github.com/MesTechSync/MesTech" \
      org.opencontainers.image.title="MesTech Blazor" \
      org.opencontainers.image.description="MesTech Blazor SaaS Frontend" \
      org.opencontainers.image.vendor="MesTech"

# Health check — /health endpoint
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s \
    CMD wget -qO- http://localhost:3200/health || exit 1

ENTRYPOINT ["dotnet", "MesTech.Blazor.dll"]
