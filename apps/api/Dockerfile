# Base runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["apps/api/src/ImportControlTower.Domain/ImportControlTower.Domain.csproj", "apps/api/src/ImportControlTower.Domain/"]
COPY ["apps/api/src/ImportControlTower.Application/ImportControlTower.Application.csproj", "apps/api/src/ImportControlTower.Application/"]
COPY ["apps/api/src/ImportControlTower.Infrastructure/ImportControlTower.Infrastructure.csproj", "apps/api/src/ImportControlTower.Infrastructure/"]
COPY ["apps/api/src/ImportControlTower.Api/ImportControlTower.Api.csproj", "apps/api/src/ImportControlTower.Api/"]
COPY ["tests/api-unit/ImportControlTower.Api.UnitTests.csproj", "tests/api-unit/"]
COPY ["tests/api-integration/ImportControlTower.Api.IntegrationTests.csproj", "tests/api-integration/"]
COPY ["apps/api/ImportControlTower.sln", "apps/api/"]

RUN dotnet restore "apps/api/ImportControlTower.sln"

# Copy source code and build
COPY apps/api/ apps/api/
COPY tests/ tests/

WORKDIR "/src/apps/api/src/ImportControlTower.Api"
RUN dotnet build "ImportControlTower.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ImportControlTower.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final production stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImportControlTower.Api.dll"]
