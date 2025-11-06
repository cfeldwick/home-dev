# Elastic Stack Samples

Examples for working with Elastic Stack, including .NET applications with structured logging and mock Elastic servers.

## Contents

### .NET Web Application

**Location:** `WebApplication/`

ASP.NET Core 9.0 web application demonstrating best practices for structured logging with Serilog and Elastic Common Schema (ECS).

**Key Features:**
- Serilog integration with ECS formatting
- Multiple sinks (Console, File)
- Environment-specific configuration
- Request logging middleware
- Production-ready logging setup

**Configuration Files:**
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development overrides
- `launchSettings.json` - Launch profiles

**Usage:**
```bash
cd WebApplication
dotnet run
```

### Fake Elastic Server

**Location:** `FakeElasticServer/`

A FastAPI-based mock Elastic server for local development and testing without requiring a full Elastic Stack deployment.

**File:** `main.py`

**Usage:**
```bash
cd FakeElasticServer
python main.py
```

## Use Cases

- Setting up structured logging in .NET applications
- Testing Elastic integrations locally
- Learning ECS log formatting
- Implementing application monitoring

## Dependencies

**.NET Application:**
- .NET 9.0 SDK
- Serilog packages (see .csproj)

**Mock Server:**
- Python 3.x
- FastAPI
- uvicorn
