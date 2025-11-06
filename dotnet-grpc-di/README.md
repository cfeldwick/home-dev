# .NET 8 gRPC with Dependency Injection Example

A production-quality example demonstrating advanced dependency injection patterns in a .NET 8 gRPC application.

## Overview

This sample showcases best practices for building a maintainable, testable gRPC service using:

- **Dependency Injection**: Extension methods, Options pattern, service lifetimes
- **Configuration**: Strongly-typed options with validation
- **gRPC**: ASP.NET Core gRPC services with Protocol Buffers
- **Testing**: Comprehensive unit and integration tests
- **Resilience**: Polly retry policies for HTTP clients
- **Health Checks**: Custom health check implementations

## Project Structure

```
dotnet-grpc-di/
├── src/
│   └── GrpcService/
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs    # DI extension methods
│       ├── Options/
│       │   ├── GreeterOptions.cs                 # Validated options
│       │   └── ExternalServiceOptions.cs
│       ├── Services/
│       │   ├── GreeterService.cs                 # gRPC service implementation
│       │   ├── IGreetingFormatter.cs
│       │   ├── GreetingFormatter.cs
│       │   ├── ITimestampProvider.cs
│       │   ├── UtcTimestampProvider.cs
│       │   ├── IExternalServiceClient.cs
│       │   ├── ExternalServiceClient.cs
│       │   └── GreeterHealthCheck.cs
│       ├── Protos/
│       │   └── greet.proto                       # Protocol Buffer definitions
│       ├── Program.cs                            # Application entry point
│       ├── appsettings.json
│       └── GrpcService.csproj
└── tests/
    ├── GrpcService.UnitTests/
    │   ├── ServiceCollectionExtensionsTests.cs   # Unit tests
    │   └── GrpcService.UnitTests.csproj
    └── GrpcService.IntegrationTests/
        ├── Fakes/
        │   └── FakeExternalServiceClient.cs      # Test double for external service
        ├── GrpcServiceIntegrationTests.cs        # Integration tests
        └── GrpcService.IntegrationTests.csproj
```

## Key Features

### 1. ServiceCollectionExtensions.cs

Demonstrates multiple DI patterns:

- **Options Pattern**: `AddOptions().Bind().ValidateDataAnnotations().ValidateOnStart()`
- **Idempotent Registration**: Using `TryAddScoped`, `TryAddSingleton`
- **Named HttpClient**: With Polly retry policies
- **Health Checks**: Custom health check registration
- **Service Lifetimes**: Proper use of Singleton, Scoped, and Transient

### 2. Options with Validation

Strongly-typed configuration classes with DataAnnotations:

```csharp
public sealed class GreeterOptions
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string GreetingPrefix { get; set; } = "Hello";

    [Required]
    [RegularExpression(@"^\d+\.\d+\.\d+$")]
    public string AppVersion { get; set; } = "1.0.0";

    [Range(1, 200)]
    public int MaxNameLength { get; set; } = 100;
}
```

### 3. Program.cs

Minimal hosting with ASP.NET Core:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.MapGrpcService<GreeterService>();
app.MapHealthChecks("/health");

app.Run();
```

### 4. Comprehensive Tests

#### Unit Tests
- Verify service registrations and lifetimes
- Test options validation
- Assert idempotency
- Validate dependency graph with `ValidateOnBuild` and `ValidateScopes`

#### Integration Tests
- Use `WebApplicationFactory<Program>` for end-to-end testing
- Test gRPC calls using `Grpc.Net.Client`
- Verify health checks
- Test concurrent requests
- Override configuration for test scenarios

## Running the Application

### Prerequisites

- .NET 8 SDK
- Your favorite IDE (Visual Studio, VS Code, Rider)

### Build and Run

```bash
# Navigate to the project directory
cd dotnet-grpc-di/src/GrpcService

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

The gRPC service will start on `http://localhost:5000` and `https://localhost:5001`.

### Testing with grpcurl

```bash
# List available services
grpcurl -plaintext localhost:5000 list

# Call SayHello
grpcurl -plaintext -d '{"name": "World"}' localhost:5000 greet.Greeter/SayHello

# Call SayHelloWithMetadata
grpcurl -plaintext -d '{"name": "World"}' localhost:5000 greet.Greeter/SayHelloWithMetadata

# Check health
curl http://localhost:5000/health
```

## Running Tests

### Unit Tests

```bash
cd tests/GrpcService.UnitTests
dotnet test --logger "console;verbosity=detailed"
```

### Integration Tests

```bash
cd tests/GrpcService.IntegrationTests
dotnet test --logger "console;verbosity=detailed"
```

### Run All Tests with Coverage

```bash
# From the solution root
dotnet test --collect:"XPlat Code Coverage"
```

## Testing with Fakes/Mocks

This example demonstrates **production-quality patterns for replacing external dependencies in integration tests** using the **Test Double pattern** (specifically, a Fake).

### The Problem

In integration tests, you often need to test your application without calling real external services because:
- External services may not be available in test environments
- They may be slow, unreliable, or expensive to call
- You want deterministic test results
- You need to test failure scenarios

### The Solution: FakeExternalServiceClient

The example includes `FakeExternalServiceClient` in the `tests/.../Fakes` directory that implements `IExternalServiceClient` with controllable behavior.

#### Key Features:

1. **Pre-configured test data**: Contains a dictionary of nickname mappings
2. **Configurable behavior**: Methods to add nicknames, set health status, etc.
3. **Same interface**: Implements `IExternalServiceClient` so it's a drop-in replacement

#### Replacing Services in Integration Tests

In `GrpcServiceIntegrationTests.cs`, the fake is registered using `ConfigureServices`:

```csharp
public GrpcServiceIntegrationTests(WebApplicationFactory<Program> factory)
{
    // Create the fake external service
    _fakeExternalService = new FakeExternalServiceClient();

    _factory = factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real service and add the fake
            services.RemoveAll<IExternalServiceClient>();
            services.AddSingleton<IExternalServiceClient>(_fakeExternalService);
        });
    });
}
```

#### Writing Tests with the Fake

The fake can be controlled per test:

```csharp
[Fact]
public async Task SayHello_WithKnownNickname_UsesNickname()
{
    // The fake has pre-configured nicknames: "William" -> "Bill"
    var request = new HelloRequest { Name = "William" };
    var response = await client.SayHelloAsync(request);

    // Asserts that the service used the nickname from the fake
    response.Message.Should().Be("Test Hello, Bill!");
}

[Fact]
public async Task SayHello_WithCustomNickname_UsesCustomMapping()
{
    // Configure fake behavior for this specific test
    _fakeExternalService.AddNickname("TestUser", "Tester");

    var request = new HelloRequest { Name = "TestUser" };
    var response = await client.SayHelloAsync(request);

    response.Message.Should().Be("Test Hello, Tester!");
}
```

### Benefits of This Pattern

✅ **Fast**: No network calls to external services
✅ **Reliable**: Deterministic behavior in tests
✅ **Flexible**: Easy to test different scenarios (success, failure, edge cases)
✅ **Isolated**: Tests don't depend on external service availability
✅ **Realistic**: Tests still go through the full application pipeline

### Alternative Approaches

While this example uses a **Fake** (a lightweight implementation with working logic), you could also use:

- **Mocks** (e.g., `Moq`, `NSubstitute`) for more dynamic test doubles
- **Stubs** for simpler scenarios where you just return fixed values
- **Test containers** (e.g., Testcontainers) for more realistic integration tests with actual services

The Fake pattern shown here is ideal for:
- Services with complex behavior that you want to control
- Scenarios where you need to verify interactions across multiple tests
- When you want readable test code without mocking framework syntax

## Configuration

### appsettings.json

```json
{
  "Greeter": {
    "GreetingPrefix": "Hello",
    "AppVersion": "1.0.0",
    "IncludeMetadata": true,
    "MaxNameLength": 100
  },
  "ExternalService": {
    "BaseUrl": "https://api.example.com",
    "ApiKey": "your-api-key-here",
    "TimeoutSeconds": 30,
    "EnableRetry": true,
    "MaxRetryAttempts": 3
  }
}
```

### Environment Variables

Override configuration using environment variables:

```bash
export Greeter__GreetingPrefix="Hi"
export ExternalService__ApiKey="production-key"
dotnet run
```

## Key Patterns Demonstrated

### 1. Extension Method Pattern

```csharp
public static IServiceCollection AddGreeterFeature(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddOptions<GreeterOptions>()
        .Bind(configuration.GetSection(GreeterOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.TryAddScoped<IGreetingFormatter, GreetingFormatter>();

    return services;
}
```

### 2. Named HttpClient with Polly

```csharp
services.AddHttpClient("ExternalService", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ExternalServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
})
.AddPolicyHandler((sp, _) =>
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(retryCount: 3, ...);
});
```

### 3. Options Validation

Options are validated:
- At binding time (DataAnnotations)
- At startup (`ValidateOnStart()`)
- On every resolution (`ValidateDataAnnotations()`)

### 4. Health Checks

```csharp
services.AddHealthChecks()
    .AddCheck<GreeterHealthCheck>("greeter_health_check");
```

## Learning Points

1. **Service Lifetimes**
   - `Singleton`: Lives for the application lifetime (e.g., `IExternalServiceClient`)
   - `Scoped`: Lives for the request lifetime (e.g., `IGreetingFormatter`)
   - `Transient`: New instance every time (not used in this example)

2. **Options Pattern**
   - Strongly-typed configuration
   - Validation with DataAnnotations
   - `ValidateOnStart()` for early failure detection (new in .NET 8)

3. **Idempotency**
   - Use `TryAdd*` methods to prevent duplicate registrations
   - Safe to call extension methods multiple times

4. **Testing**
   - Unit tests verify DI configuration
   - Integration tests verify end-to-end behavior
   - Use `WebApplicationFactory` for in-memory testing
   - **Fake/Mock external dependencies** using `services.RemoveAll<T>()` and `services.AddSingleton<T>(fake)`
   - Test doubles (Fakes) provide controlled, deterministic behavior

5. **Mocking/Faking External Services**
   - Use the same interface (`IExternalServiceClient`)
   - Replace real implementation with test double in `ConfigureServices`
   - Control behavior per test (add test data, simulate failures)
   - No external dependencies in tests = fast, reliable tests

6. **gRPC in .NET**
   - Protocol Buffers for service contracts
   - HTTP/2 for transport
   - Built-in support in ASP.NET Core

## Best Practices Demonstrated

✅ Null checks with `ArgumentNullException.ThrowIfNull()`
✅ Sealed classes where appropriate
✅ Interface segregation
✅ Dependency injection over service locator
✅ Configuration validation at startup
✅ Comprehensive logging
✅ Proper exception handling
✅ HTTP client factory pattern
✅ Retry policies with Polly
✅ Health checks for monitoring
✅ Full test coverage (unit + integration)
✅ Test doubles (Fakes) for external dependencies
✅ Service replacement in integration tests using `RemoveAll` and `AddSingleton`

## References

- [ASP.NET Core Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [Options Pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- [gRPC in .NET](https://learn.microsoft.com/en-us/aspnet/core/grpc/)
- [Integration Tests with WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Polly Resilience Policies](https://github.com/App-vNext/Polly)

## License

This is a code sample for educational purposes.
