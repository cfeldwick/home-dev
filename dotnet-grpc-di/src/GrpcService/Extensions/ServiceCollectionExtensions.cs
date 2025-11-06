using GrpcService.Options;
using GrpcService.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace GrpcService.Extensions;

/// <summary>
/// Extension methods for configuring services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Greeter-related services to the service collection.
    /// Demonstrates: Options pattern, validation, TryAdd, scoped services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGreeterFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure and validate options using the Options pattern
        services.AddOptions<GreeterOptions>()
            .Bind(configuration.GetSection(GreeterOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart(); // Validates options at application startup (new in .NET 8)

        // Use TryAddScoped to make registration idempotent
        services.TryAddScoped<IGreetingFormatter, GreetingFormatter>();
        services.TryAddScoped<ITimestampProvider, UtcTimestampProvider>();

        return services;
    }

    /// <summary>
    /// Adds HTTP client for external service integration.
    /// Demonstrates: Named HttpClient, options-based configuration, Polly retry policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExternalServiceClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure and validate external service options
        services.AddOptions<ExternalServiceOptions>()
            .Bind(configuration.GetSection(ExternalServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register named HttpClient with configuration from options
        services.AddHttpClient("ExternalService", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExternalServiceOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            client.DefaultRequestHeaders.Add("User-Agent", "GrpcService/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5), // DNS refresh
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
        })
        .AddPolicyHandler((serviceProvider, _) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExternalServiceOptions>>().Value;

            if (!options.EnableRetry)
            {
                return Policy.NoOpAsync<HttpResponseMessage>();
            }

            // Exponential backoff retry policy
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: options.MaxRetryAttempts,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<ExternalServiceClient>>();
                        logger.LogWarning(
                            "Retry {RetryCount} after {Delay}ms delay due to {Exception}",
                            retryCount,
                            timespan.TotalMilliseconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        });

        // Register the client wrapper using TryAddSingleton for idempotency
        services.TryAddSingleton<IExternalServiceClient, ExternalServiceClient>();

        return services;
    }

    /// <summary>
    /// Adds health checks for the application.
    /// Demonstrates: Health check registration, custom checks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck<GreeterHealthCheck>("greeter_health_check");

        return services;
    }

    /// <summary>
    /// Adds all application services in one convenient method.
    /// This is the primary entry point for configuring the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddGreeterFeature(configuration);
        services.AddExternalServiceClient(configuration);
        services.AddApplicationHealthChecks();

        return services;
    }
}
