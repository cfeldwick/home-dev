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

    /// <summary>
    /// Gets a nickname for the given name from the external service.
    /// </summary>
    /// <param name="name">The name to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The nickname if found, otherwise null.</returns>
    Task<string?> GetNicknameAsync(string name, CancellationToken cancellationToken = default);
}
