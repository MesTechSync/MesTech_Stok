# ─────────────────────────────────────────────────────────────
# MesTech WebAPI — Multi-stage Docker build (Production)
# Target: Coolify / Docker Compose deployment on port 5100
# Base: .NET 9.0 SDK (build) + ASP.NET 9.0 Runtime (run)
# ─────────────────────────────────────────────────────────────

# ── Stage 1: Restore (layer-cached) ──
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
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
RUN dotnet restore src/MesTech.WebApi/MesTech.WebApi.csproj

# Copy full source (changes frequently — this layer rebuilds on code change)
COPY MesTech_Stok/MesTechStok/src/ ./src/

# ── Stage 2: Publish ──
FROM build AS publish
RUN dotnet publish src/MesTech.WebApi/MesTech.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Stage 3: Runtime (minimal attack surface) ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Security: run as non-root user
RUN addgroup -S mestech && adduser -S mestech -G mestech
USER mestech

# Copy published output from build stage
COPY --from=publish /app/publish .

# Expose WebAPI port
EXPOSE 5100

# ASP.NET Core configuration
ENV ASPNETCORE_URLS=http://+:5100
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check — uses existing /health endpoint (interval 30s, timeout 5s, 3 retries)
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5100/health || exit 1

ENTRYPOINT ["dotnet", "MesTech.WebApi.dll"]
