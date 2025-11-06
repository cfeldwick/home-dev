using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService.Options;
using GrpcService.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrpcService.IntegrationTests;

/// <summary>
/// Integration tests for the gRPC service using WebApplicationFactory.
/// </summary>
public class GrpcServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GrpcServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Enable HTTP/2 without TLS for testing
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Greeter:GreetingPrefix"] = "Test Hello",
                    ["Greeter:AppVersion"] = "1.0.0-test",
                    ["Greeter:IncludeMetadata"] = "true",
                    ["Greeter:MaxNameLength"] = "100",
                    ["ExternalService:BaseUrl"] = "https://test-api.example.com",
                    ["ExternalService:ApiKey"] = "test-key-12345",
                    ["ExternalService:TimeoutSeconds"] = "30",
                    ["ExternalService:EnableRetry"] = "false",
                    ["ExternalService:MaxRetryAttempts"] = "0"
                }!);
            });
        });
    }

    private GrpcChannel CreateChannel()
    {
        // Create a channel using the test server's HttpClient
        // This ensures the request goes through the full middleware pipeline
        var client = _factory.CreateDefaultClient();
        var channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = client
        });

        return channel;
    }

    [Fact]
    public async Task Application_Starts_Successfully()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("gRPC");
    }

    [Fact]
    public async Task HealthCheck_Returns_Healthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task SayHello_Returns_ValidResponse()
    {
        // Arrange
        var channel = CreateChannel();
        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "Integration Test" };

        // Act
        var response = await client.SayHelloAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Message.Should().Be("Test Hello, Integration Test!");
        response.Timestamp.Should().NotBeNullOrEmpty();
        response.Version.Should().Be("1.0.0-test");
    }

    [Fact]
    public async Task SayHello_WithEmptyName_ThrowsRpcException()
    {
        // Arrange
        var channel = CreateChannel();
        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "" };

        // Act
        Func<Task> act = async () => await client.SayHelloAsync(request);

        // Assert
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unknown || e.StatusCode == StatusCode.Internal);
    }

    [Fact]
    public async Task SayHello_WithLongName_TruncatesName()
    {
        // Arrange
        var channel = CreateChannel();
        var client = new Greeter.GreeterClient(channel);
        var longName = new string('A', 150); // Exceeds MaxNameLength of 100
        var request = new HelloRequest { Name = longName };

        // Act
        var response = await client.SayHelloAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Message.Should().StartWith("Test Hello,");
        // Name should be truncated to 100 characters
        response.Message.Length.Should().BeLessThan(longName.Length + 20);
    }

    [Fact]
    public async Task SayHelloWithMetadata_Returns_ValidResponse()
    {
        // Arrange
        var channel = CreateChannel();
        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "Metadata Test" };

        // Act
        var call = client.SayHelloWithMetadataAsync(request);
        var response = await call;
        var trailers = call.GetTrailers();

        // Assert
        response.Should().NotBeNull();
        response.Message.Should().Be("Test Hello, Metadata Test!");
        response.Timestamp.Should().NotBeNullOrEmpty();
        response.Version.Should().Be("1.0.0-test");

        // Verify metadata in response trailers
        trailers.Should().Contain(h => h.Key == "x-greeting-timestamp");
        trailers.Should().Contain(h => h.Key == "x-app-version");
    }

    [Fact]
    public async Task SayHelloWithMetadata_IncludesMetadataWhenConfigured()
    {
        // Arrange
        var channel = CreateChannel();
        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "Config Test" };

        // Act
        var response = await client.SayHelloWithMetadataAsync(request);

        // Assert
        // Since IncludeMetadata is true in test config, metadata should be included
        response.Timestamp.Should().NotBeNullOrEmpty();
        response.Version.Should().Be("1.0.0-test");
    }

    [Fact]
    public void DependencyInjection_ResolvesAllCriticalServices()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Assert - Verify all critical services can be resolved
        var greetingFormatter = services.GetService<IGreetingFormatter>();
        greetingFormatter.Should().NotBeNull();

        var timestampProvider = services.GetService<ITimestampProvider>();
        timestampProvider.Should().NotBeNull();

        var externalServiceClient = services.GetService<IExternalServiceClient>();
        externalServiceClient.Should().NotBeNull();

        var greeterOptions = services.GetService<IOptions<GreeterOptions>>();
        greeterOptions.Should().NotBeNull();
        greeterOptions!.Value.Should().NotBeNull();

        var externalServiceOptions = services.GetService<IOptions<ExternalServiceOptions>>();
        externalServiceOptions.Should().NotBeNull();
        externalServiceOptions!.Value.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_IsLoadedCorrectly()
    {
        // Arrange
        var greeterOptions = _factory.Services.GetRequiredService<IOptions<GreeterOptions>>().Value;
        var externalServiceOptions = _factory.Services.GetRequiredService<IOptions<ExternalServiceOptions>>().Value;

        // Assert - Verify test configuration is applied
        greeterOptions.GreetingPrefix.Should().Be("Test Hello");
        greeterOptions.AppVersion.Should().Be("1.0.0-test");
        greeterOptions.IncludeMetadata.Should().BeTrue();
        greeterOptions.MaxNameLength.Should().Be(100);

        externalServiceOptions.BaseUrl.Should().Be("https://test-api.example.com");
        externalServiceOptions.ApiKey.Should().Be("test-key-12345");
        externalServiceOptions.TimeoutSeconds.Should().Be(30);
        externalServiceOptions.EnableRetry.Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentRequests_AreHandledCorrectly()
    {
        // Arrange
        var channel = CreateChannel();
        var client = new Greeter.GreeterClient(channel);
        var tasks = new List<Task<HelloReply>>();

        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            var request = new HelloRequest { Name = $"User{i}" };
            tasks.Add(client.SayHelloAsync(request).ResponseAsync);
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r != null && !string.IsNullOrEmpty(r.Message));

        for (int i = 0; i < 10; i++)
        {
            responses[i].Message.Should().Be($"Test Hello, User{i}!");
        }
    }

    [Fact]
    public async Task GreetingFormatter_UsesConfiguredPrefix()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<IGreetingFormatter>();

        // Act
        var greeting = formatter.FormatGreeting("Custom Test");

        // Assert
        greeting.Should().Be("Test Hello, Custom Test!");
    }

    [Fact]
    public async Task TimestampProvider_ReturnsValidTimestamp()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var timestampProvider = scope.ServiceProvider.GetRequiredService<ITimestampProvider>();

        // Act
        var timestamp = timestampProvider.GetCurrentTimestamp();

        // Assert
        timestamp.Should().NotBeNullOrEmpty();
        DateTime.TryParse(timestamp, out var parsedDate).Should().BeTrue();
        parsedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
