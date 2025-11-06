using System.ComponentModel.DataAnnotations;

namespace GrpcService.Options;

/// <summary>
/// Configuration options for external service integrations.
/// </summary>
public sealed class ExternalServiceOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "ExternalService";

    /// <summary>
    /// Base URL for the external service API
    /// </summary>
    [Required(ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    [Required(ErrorMessage = "ApiKey is required")]
    [MinLength(10, ErrorMessage = "ApiKey must be at least 10 characters")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Timeout for HTTP requests in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable retry policy
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetryAttempts must be between 0 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;
}
