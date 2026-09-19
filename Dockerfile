# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and project files first so restore is cached
COPY src/GitHubStats.sln src/
COPY src/Directory.Build.props src/
COPY src/GitHubStats.Api/*.csproj src/GitHubStats.Api/
COPY src/GitHubStats.Application/*.csproj src/GitHubStats.Application/
COPY src/GitHubStats.Domain/*.csproj src/GitHubStats.Domain/
COPY src/GitHubStats.Infrastructure/*.csproj src/GitHubStats.Infrastructure/
COPY src/GitHubStats.Rendering/*.csproj src/GitHubStats.Rendering/
COPY src/GitHubStats.Tests/*.csproj src/GitHubStats.Tests/
RUN dotnet restore src/GitHubStats.sln

COPY src/ src/
RUN dotnet publish src/GitHubStats.Api/GitHubStats.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1
USER app
ENTRYPOINT ["dotnet", "GitHubStats.Api.dll"]
