# ==============================================================================
# ECommerce API — Multi-stage Docker build
# ==============================================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ECommerce.slnx ./
COPY src/ECommerce.Api/ECommerce.Api.csproj src/ECommerce.Api/
COPY src/ECommerce.Application/ECommerce.Application.csproj src/ECommerce.Application/
COPY src/ECommerce.Domain/ECommerce.Domain.csproj src/ECommerce.Domain/
COPY src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj src/ECommerce.Infrastructure/
COPY tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj tests/ECommerce.UnitTests/
COPY tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj tests/ECommerce.IntegrationTests/
COPY tests/ECommerce.ArchitectureTests/ECommerce.ArchitectureTests.csproj tests/ECommerce.ArchitectureTests/

# Restore dependencies (cached unless .csproj files change)
RUN dotnet restore

# Copy all source code
COPY . .

# Publish the API project
RUN dotnet publish src/ECommerce.Api/ECommerce.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Create uploads directory
RUN mkdir -p /app/uploads && chown -R appuser:appuser /app

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
