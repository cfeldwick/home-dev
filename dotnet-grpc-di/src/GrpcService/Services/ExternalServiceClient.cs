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

    public async Task<string?> GetNicknameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Attempted to get nickname with null or empty name");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ExternalService");

            var response = await client.GetAsync($"/api/nicknames/{Uri.EscapeDataString(name)}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Nickname not found for {Name}: {StatusCode}",
                    name,
                    response.StatusCode);

                return null;
            }

            var nickname = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Found nickname for {Name}: {Nickname}", name, nickname);

            return nickname;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get nickname for {Name}", name);
            return null;
        }
    }
}
