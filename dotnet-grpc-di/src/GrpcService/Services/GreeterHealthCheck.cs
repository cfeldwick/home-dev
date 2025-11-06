using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcService.Services;

/// <summary>
/// Health check for the Greeter service.
/// </summary>
public sealed class GreeterHealthCheck : IHealthCheck
{
    private readonly IExternalServiceClient _externalServiceClient;
    private readonly ILogger<GreeterHealthCheck> _logger;

    public GreeterHealthCheck(
        IExternalServiceClient externalServiceClient,
        ILogger<GreeterHealthCheck> logger)
    {
        _externalServiceClient = externalServiceClient ?? throw new ArgumentNullException(nameof(externalServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if external service is available
            var isExternalServiceHealthy = await _externalServiceClient.HealthCheckAsync(cancellationToken);

            if (!isExternalServiceHealthy)
            {
                _logger.LogWarning("External service is unhealthy");
                return HealthCheckResult.Degraded(
                    "External service is unavailable",
                    data: new Dictionary<string, object>
                    {
                        ["external_service"] = "unhealthy"
                    });
            }

            return HealthCheckResult.Healthy("All dependencies are healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
