# ─────────────────────────────────────────────────────────────
# MesTech WebAPI — Multi-stage Docker build (Production)
# Target: Coolify / Docker Compose deployment on port 3100
# Base: .NET 9.0 SDK Alpine (build) + ASP.NET 9.0 Runtime Alpine (run)
# Image size target: <150MB
#
# Build context: mono-repo root (e.g. docker build -f MesTech_Stok/MesTechStok/src/MesTech.WebApi/Dockerfile .)
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore (layer-cached) ──
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS restore
WORKDIR /src

# Copy solution and build props first for optimal layer caching
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
COPY MesTech_Stok/MesTechStok/src/MesTech.Tests.Unit/MesTech.Tests.Unit.csproj ./src/MesTech.Tests.Unit/
COPY MesTech_Stok/MesTechStok/src/MesTech.Tests.Integration/MesTech.Tests.Integration.csproj ./src/MesTech.Tests.Integration/
COPY MesTech_Stok/MesTechStok/src/MesTech.Tests.Architecture/MesTech.Tests.Architecture.csproj ./src/MesTech.Tests.Architecture/

# Restore only WebApi project (pulls transitive deps via solution graph)
RUN dotnet restore src/MesTech.WebApi/MesTech.WebApi.csproj \
    --runtime linux-musl-x64

# ── Stage 2: Build + Publish ──
FROM restore AS publish
# Copy full source (changes frequently — this layer rebuilds on code change)
COPY MesTech_Stok/MesTechStok/src/ ./src/

RUN dotnet publish src/MesTech.WebApi/MesTech.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --runtime linux-musl-x64 \
    --self-contained false \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# ── Stage 3: Runtime (minimal attack surface) ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Install wget for healthcheck (busybox wget already in alpine, but ensure it exists)
# Alpine ships busybox with wget built-in — no extra install needed

# Security: non-root user
RUN addgroup -S mestech && adduser -S mestech -G mestech \
    && mkdir -p /app \
    && chown -R mestech:mestech /app

WORKDIR /app

# Copy published output from build stage (owned by mestech)
COPY --from=publish --chown=mestech:mestech /app/publish .

# Switch to non-root user
USER mestech

# Expose WebAPI port
EXPOSE 3100

# ASP.NET Core configuration
ENV ASPNETCORE_URLS=http://+:3100 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

# Labels for container metadata
LABEL org.opencontainers.image.source="https://github.com/MesTechSync/MesTech" \
      org.opencontainers.image.title="MesTech WebAPI" \
      org.opencontainers.image.description="MesTech Stok Management WebAPI" \
      org.opencontainers.image.vendor="MesTech"

# Health check — /health endpoint (30s interval, 10s timeout, 3 retries, 40s startup grace)
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s \
    CMD wget -qO- http://localhost:3100/health || exit 1

ENTRYPOINT ["dotnet", "MesTech.WebApi.dll"]
