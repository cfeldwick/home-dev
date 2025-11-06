namespace GrpcService.Services;

/// <summary>
/// Client for interacting with external services via HTTP.
/// </summary>
public sealed class ExternalServiceClient : IExternalServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalServiceClient> _logger;

    public ExternalServiceClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ExternalService");

            var response = await client.GetAsync("/health", cancellationToken);

            var isHealthy = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "External service health check: {Status} ({StatusCode})",
                isHealthy ? "Healthy" : "Unhealthy",
                response.StatusCode);

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External service health check failed");
            return false;
        }
    }
}
