namespace GrpcService.Services;

/// <summary>
/// Client for interacting with external services.
/// </summary>
public interface IExternalServiceClient
{
    /// <summary>
    /// Performs a health check against the external service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is healthy, false otherwise.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
