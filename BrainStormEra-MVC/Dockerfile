# BrainStormEra MVC Application Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5274

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY BrainStormEra.sln .
COPY BrainStormEra-MVC/BrainStormEra-MVC.csproj BrainStormEra-MVC/
COPY BusinessLogicLayer/BusinessLogicLayer.csproj BusinessLogicLayer/
COPY DataAccessLayer/DataAccessLayer.csproj DataAccessLayer/

# Restore dependencies
RUN dotnet restore BrainStormEra-MVC/BrainStormEra-MVC.csproj

# Copy all source code
COPY . .

# Build the application
WORKDIR /src/BrainStormEra-MVC
RUN dotnet build BrainStormEra-MVC.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish BrainStormEra-MVC.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create SharedMedia directory
RUN mkdir -p /app/SharedMedia

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5274
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:5274/health || curl -f http://localhost:5274 || exit 1

# Run the application
ENTRYPOINT ["dotnet", "BrainStormEra-MVC.dll"]
