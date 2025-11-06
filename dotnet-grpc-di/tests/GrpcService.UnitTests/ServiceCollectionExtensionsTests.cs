using FluentAssertions;
using GrpcService.Extensions;
using GrpcService.Options;
using GrpcService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrpcService.UnitTests;

public class ServiceCollectionExtensionsTests
{
    private static IConfiguration CreateConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static IConfiguration CreateValidConfiguration()
    {
        return CreateConfiguration(new Dictionary<string, string?>
        {
            ["Greeter:GreetingPrefix"] = "Hello",
            ["Greeter:AppVersion"] = "1.0.0",
            ["Greeter:IncludeMetadata"] = "true",
            ["Greeter:MaxNameLength"] = "100",
            ["ExternalService:BaseUrl"] = "https://api.example.com",
            ["ExternalService:ApiKey"] = "test-api-key-12345",
            ["ExternalService:TimeoutSeconds"] = "30",
            ["ExternalService:EnableRetry"] = "true",
            ["ExternalService:MaxRetryAttempts"] = "3"
        });
    }

    [Fact]
    public void AddGreeterFeature_RegistersOptionsWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        services.AddGreeterFeature(configuration);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        var options = provider.GetService<IOptions<GreeterOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
        options.Value.GreetingPrefix.Should().Be("Hello");
        options.Value.AppVersion.Should().Be("1.0.0");
        options.Value.IncludeMetadata.Should().BeTrue();
        options.Value.MaxNameLength.Should().Be(100);
    }

    [Fact]
    public void AddGreeterFeature_RegistersGreetingFormatterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Act
        services.AddGreeterFeature(configuration);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IGreetingFormatter));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<GreetingFormatter>();
    }

    [Fact]
    public void AddGreeterFeature_RegistersTimestampProviderAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        services.AddGreeterFeature(configuration);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITimestampProvider));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<UtcTimestampProvider>();
    }

    [Fact]
    public void AddGreeterFeature_IsIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();

        // Act
        services.AddGreeterFeature(configuration);
        var countAfterFirst = services.Count(d => d.ServiceType == typeof(IGreetingFormatter));

        services.AddGreeterFeature(configuration);
        var countAfterSecond = services.Count(d => d.ServiceType == typeof(IGreetingFormatter));

        // Assert
        countAfterFirst.Should().Be(1);
        countAfterSecond.Should().Be(1, "TryAddScoped should prevent duplicate registrations");
    }

    [Fact]
    public void AddGreeterFeature_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Arrange
        IServiceCollection services = null!;
        var configuration = CreateValidConfiguration();

        // Act & Assert
        var act = () => services.AddGreeterFeature(configuration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddGreeterFeature_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = null!;

        // Act & Assert
        var act = () => services.AddGreeterFeature(configuration);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void AddGreeterFeature_ValidatesOptionsOnStart()
    {
        // Arrange
        var services = new ServiceCollection();
        var invalidConfig = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Greeter:GreetingPrefix"] = "", // Invalid: empty string
            ["Greeter:AppVersion"] = "invalid", // Invalid: wrong format
            ["Greeter:MaxNameLength"] = "0" // Invalid: out of range
        });

        services.AddGreeterFeature(invalidConfig);

        // Act & Assert
        var act = () => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });

        act.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().NotBeEmpty();
    }

    [Fact]
    public void AddExternalServiceClient_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Act
        services.AddExternalServiceClient(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = provider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();

        var client = httpClientFactory!.CreateClient("ExternalService");
        client.Should().NotBeNull();
        client.BaseAddress.Should().Be(new Uri("https://api.example.com"));
        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        client.DefaultRequestHeaders.Contains("X-API-Key").Should().BeTrue();
    }

    [Fact]
    public void AddExternalServiceClient_RegistersExternalServiceClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Act
        services.AddExternalServiceClient(configuration);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IExternalServiceClient));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<ExternalServiceClient>();
    }

    [Fact]
    public void AddExternalServiceClient_IsIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Act
        services.AddExternalServiceClient(configuration);
        var countAfterFirst = services.Count(d => d.ServiceType == typeof(IExternalServiceClient));

        services.AddExternalServiceClient(configuration);
        var countAfterSecond = services.Count(d => d.ServiceType == typeof(IExternalServiceClient));

        // Assert
        countAfterFirst.Should().Be(1);
        countAfterSecond.Should().Be(1, "TryAddSingleton should prevent duplicate registrations");
    }

    [Fact]
    public void AddExternalServiceClient_ValidatesOptionsDataAnnotations()
    {
        // Arrange
        var services = new ServiceCollection();
        var invalidConfig = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ExternalService:BaseUrl"] = "not-a-url", // Invalid URL
            ["ExternalService:ApiKey"] = "short", // Too short
            ["ExternalService:TimeoutSeconds"] = "500" // Out of range
        });

        services.AddLogging();
        services.AddExternalServiceClient(invalidConfig);

        // Act & Assert
        var act = () => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });

        act.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().NotBeEmpty();
    }

    [Fact]
    public void AddApplicationHealthChecks_RegistersHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationHealthChecks();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(HealthCheckService));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationHealthChecks_RegistersGreeterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Setup dependencies for GreeterHealthCheck
        services.AddExternalServiceClient(configuration);

        // Act
        services.AddApplicationHealthChecks();
        var provider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Act
        services.AddApplicationServices(configuration);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        provider.GetService<IOptions<GreeterOptions>>().Should().NotBeNull();
        provider.GetService<IOptions<ExternalServiceOptions>>().Should().NotBeNull();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IGreetingFormatter>().Should().NotBeNull();
        scope.ServiceProvider.GetService<ITimestampProvider>().Should().NotBeNull();

        provider.GetService<IExternalServiceClient>().Should().NotBeNull();
        provider.GetService<HealthCheckService>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_BuildsValidDependencyGraph()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateValidConfiguration();
        services.AddLogging();

        // Act
        services.AddApplicationServices(configuration);

        // Assert - This will throw if the dependency graph is invalid
        var act = () => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        act.Should().NotThrow("the dependency graph should be valid");
    }
}
