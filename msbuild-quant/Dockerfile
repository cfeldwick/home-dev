# Multi-stage Dockerfile for ASP.NET Core gRPC app with quant library

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install tar for extracting quant library
RUN apt-get update && apt-get install -y tar bzip2 && rm -rf /var/lib/apt/lists/*

# Copy project files
COPY *.csproj ./
COPY QuantVersion.props ./
COPY Directory.Build.props ./
COPY Directory.Build.targets ./

# Restore dependencies (without downloading quant yet)
RUN dotnet restore

# Copy source code
COPY . .

# Download quant library during build
ARG QUANT_REPO_TOKEN
ENV QUANT_REPO_TOKEN=$QUANT_REPO_TOKEN
RUN dotnet build -c Release -p:DownloadQuant=true --no-restore

# Publish application
RUN dotnet publish -c Release -o /app/publish --no-build

# Verify quant library was downloaded
RUN ls -la .quant/ || echo "Warning: .quant directory not found"

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy quant library from build stage
COPY --from=build /src/.quant/ ./.quant/

# Set environment variable for quant library location
ARG QUANT_VERSION=100000
ENV QUANT_HOME=/app/.quant/quant-${QUANT_VERSION}
ENV QUANT_VERSION=${QUANT_VERSION}

# Expose gRPC port (default 8080 for ASP.NET 9.0)
EXPOSE 8080
EXPOSE 8081

# Health check endpoint (adjust path as needed)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set user for security (non-root)
USER $APP_UID

# Start the application
ENTRYPOINT ["dotnet", "YourApp.dll"]
