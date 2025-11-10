# MSBuild Quant Library Download System

A robust, version-managed, on-demand library download system for .NET projects using MSBuild.

## Overview

This system provides automated download, caching, and version management for the quant library in .NET builds. It's designed to work seamlessly in both local development and CI/CD environments.

## Features

- **Single Source of Truth**: Version managed through `QuantVersion.props`
- **On-Demand Downloads**: Only downloads when explicitly requested with `-p:DownloadQuant=true`
- **Smart Caching**: Uses stamp files to prevent repeated downloads
- **Cross-Platform**: Works on Windows, Linux, and macOS
- **Race Condition Protection**: Lock files prevent concurrent download conflicts
- **Authentication Support**: Integrated token-based authentication
- **Download Verification**: Validates file size and extraction success
- **Code Generation**: Auto-generates `QuantVersion.g.cs` for compile-time version access
- **CI/CD Friendly**: Outputs machine-readable version information

## File Structure

```
├── QuantVersion.props          # Version definition (single source of truth)
├── Directory.Build.props       # Property definitions and configuration
├── Directory.Build.targets     # Download automation and code generation
├── .gitlab-ci.yml             # GitLab CI/CD pipeline example
├── Dockerfile                 # Multi-stage Docker build with quant library
└── README.md                  # This file
```

## Quick Start

### 1. Configure Version

Edit `QuantVersion.props` to set your desired library version:

```xml
<Project>
  <PropertyGroup>
    <QuantLibVersion>100000</QuantLibVersion>
  </PropertyGroup>
</Project>
```

### 2. Set Authentication Token

Export the repository access token:

```bash
export QUANT_REPO_TOKEN="your-token-here"
```

### 3. Build with Download

Build your project and download the library:

```bash
dotnet build -p:DownloadQuant=true
```

The library will be downloaded to `.quant/quant-<version>/` and cached for future builds.

## Configuration Options

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `QUANT_REPO_TOKEN` | Yes* | Authentication token for repository access |

*Required only when `-p:DownloadQuant=true` is specified

### MSBuild Properties

Configure in your project or via command line:

| Property | Default | Description |
|----------|---------|-------------|
| `DownloadQuant` | `false` | Enable library download |
| `QuantLibVersion` | `100000` | Library version (from QuantVersion.props) |
| `QuantNamespace` | `Quant.Shared` | Namespace for generated code |
| `QuantDownloadTimeoutMinutes` | `10` | HTTP download timeout |
| `QuantLibUrl` | (computed) | Download URL (can be overridden) |

### Example: Custom Configuration

```bash
dotnet build \
  -p:DownloadQuant=true \
  -p:QuantNamespace=MyApp.Quant \
  -p:QuantDownloadTimeoutMinutes=15
```

## Generated Code

The system automatically generates `QuantVersion.g.cs` for C# projects:

```csharp
namespace Quant.Shared
{
    public static class QuantVersion
    {
        public const int Value = 100000;
    }
}
```

Access the version in your code:

```csharp
using Quant.Shared;

Console.WriteLine($"Quant Library Version: {QuantVersion.Value}");
```

## Directory Structure After Download

```
.quant/
├── quant-100000.tar.bz2        # Downloaded archive
├── quant-100000/               # Extracted library
│   ├── _ok.stamp              # Success marker
│   └── [library files]
└── _download.lock             # Lock file (temporary during download)
```

## GitLab CI/CD Integration

### Basic Pipeline

Copy `.gitlab-ci.yml` to your project root and customize:

```yaml
stages:
  - download
  - build
  - test
  - package

variables:
  QUANT_CACHE_KEY: "quant-${CI_COMMIT_REF_SLUG}"

download-quant:
  stage: download
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet build -p:DownloadQuant=true -t:DownloadQuantOnRestore
  cache:
    key: $QUANT_CACHE_KEY
    paths:
      - .quant/
  artifacts:
    paths:
      - .quant/
```

### Using in Tests

```yaml
test:
  stage: test
  dependencies:
    - download-quant
  script:
    - export QUANT_VERSION=$(grep -oP '(?<=<QuantLibVersion>)\d+(?=</QuantLibVersion>)' QuantVersion.props)
    - export QUANT_HOME=$(pwd)/.quant/quant-$QUANT_VERSION
    - dotnet test -c Release
```

### Docker Build Integration

```yaml
package:docker:
  stage: package
  image: docker:24
  dependencies:
    - build
  script:
    - docker build --build-arg QUANT_REPO_TOKEN=$QUANT_REPO_TOKEN -t myapp:latest .
    - docker push myapp:latest
```

## Docker Usage

### Building the Image

```bash
docker build \
  --build-arg QUANT_REPO_TOKEN=$QUANT_REPO_TOKEN \
  --build-arg QUANT_VERSION=100000 \
  -t myapp:latest .
```

### Running the Container

```bash
docker run -p 8080:8080 \
  -e QUANT_HOME=/app/.quant/quant-100000 \
  -e QUANT_VERSION=100000 \
  myapp:latest
```

### Multi-Stage Build Benefits

The included Dockerfile uses multi-stage builds to:
1. Download and build with the quant library in the SDK image
2. Copy only the extracted library to the lean runtime image
3. Minimize final image size while keeping the library available

## Local Development

### First Build

```bash
# Download the library
dotnet build -p:DownloadQuant=true

# Subsequent builds use cached library
dotnet build
```

### Force Re-Download

```bash
# Delete the stamp file
rm -f .quant/quant-100000/_ok.stamp

# Rebuild with download
dotnet build -p:DownloadQuant=true
```

### Version Update

1. Edit `QuantVersion.props` with new version
2. Build with `-p:DownloadQuant=true`
3. Old version remains cached, new version downloaded alongside

## Troubleshooting

### Error: "QUANT_REPO_TOKEN environment variable not set"

**Solution**: Export the token before building:
```bash
export QUANT_REPO_TOKEN="your-token-here"
```

### Error: "Failed to extract... tar is not available"

**Solution**: Install tar:
- Ubuntu/Debian: `apt-get install tar bzip2`
- Alpine: `apk add tar bzip2`
- macOS: Included by default
- Windows: Install Git for Windows or WSL

### Error: "Downloaded file is empty or corrupt"

**Solution**:
1. Check network connectivity
2. Verify `QUANT_REPO_TOKEN` is valid
3. Check URL in `Directory.Build.props`
4. Delete partial download: `rm -f .quant/quant-*/*.tar.bz2`

### Concurrent Build Issues

If multiple builds run simultaneously, the lock file mechanism provides basic protection. For heavy concurrent usage, consider:
1. Pre-downloading in a dedicated pipeline stage
2. Using CI cache mechanisms
3. Mounting a shared cache volume in Docker builds

### Debugging

Enable verbose MSBuild output:
```bash
dotnet build -p:DownloadQuant=true -v:detailed
```

## Security Considerations

1. **Token Storage**: Never commit `QUANT_REPO_TOKEN` to version control
2. **GitLab CI**: Store token as a protected CI/CD variable
3. **Docker**: Pass token only as build arg, never in ENV in final image
4. **File Permissions**: Downloaded files inherit default permissions

## Performance Tips

1. **CI/CD Caching**: Always cache `.quant/` directory across pipeline stages
2. **Parallel Builds**: Stamp files prevent redundant downloads
3. **Docker Layer Caching**: Structure Dockerfile to cache quant download layer
4. **Network**: Adjust timeout for slow connections via `QuantDownloadTimeoutMinutes`

## Customization

### Custom Download URL

Override in your project file or `Directory.Build.props`:

```xml
<PropertyGroup>
  <QuantLibUrl>https://custom.repo.com/libs/quant-$(QuantLibVersion).tar.bz2</QuantLibUrl>
</PropertyGroup>
```

### Custom Cache Location

```xml
<PropertyGroup>
  <QuantLibBaseDir>/custom/path/.quant</QuantLibBaseDir>
</PropertyGroup>
```

### Skip Code Generation

Set in your project file:

```xml
<PropertyGroup>
  <GenerateQuantVersion>false</GenerateQuantVersion>
</PropertyGroup>
```

Then modify the target condition in `Directory.Build.targets`.

## Architecture

### Download Flow

1. Check if `DownloadQuant=true`
2. Verify stamp file doesn't exist
3. Validate `QUANT_REPO_TOKEN` is set
4. Acquire lock file (prevent race conditions)
5. Delete any partial downloads
6. Download tarball via HttpClient
7. Verify file size > 0
8. Extract with tar (fallback to `--bzip2` flag)
9. Create stamp file
10. Release lock file
11. Output `QUANT_VERSION` and `QUANT_HOME`

### Code Generation Flow

1. Runs `BeforeTargets="CoreCompile"` for C# projects
2. Creates `obj/QuantVersion.g.cs` (or custom path)
3. Injects version from `QuantVersion.props`
4. Adds to compilation items

## Maintenance

### Updating the System

To update the MSBuild system files:
1. Edit `Directory.Build.props` or `Directory.Build.targets`
2. Test locally with `dotnet build`
3. Commit changes
4. CI/CD will use updated logic on next run

### Version Management Strategy

- **Patch updates**: Same major version, increment last digits
- **Minor updates**: Change middle digits, test compatibility
- **Major updates**: Change first digits, expect breaking changes

## Support

For issues or questions:
1. Check troubleshooting section above
2. Enable verbose logging: `dotnet build -v:detailed`
3. Review generated files in `.quant/` directory
4. Check GitLab CI logs for pipeline failures

## License

Specify your license here.
