using System.ComponentModel.DataAnnotations;

namespace GrpcService.Options;

/// <summary>
/// Configuration options for the Greeter service.
/// </summary>
public sealed class GreeterOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Greeter";

    /// <summary>
    /// Default greeting prefix used in responses
    /// </summary>
    [Required(ErrorMessage = "GreetingPrefix is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "GreetingPrefix must be between 1 and 50 characters")]
    public string GreetingPrefix { get; set; } = "Hello";

    /// <summary>
    /// Application version to include in responses
    /// </summary>
    [Required(ErrorMessage = "AppVersion is required")]
    [RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "AppVersion must be in format X.Y.Z")]
    public string AppVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Whether to include metadata in responses
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Maximum length for user names
    /// </summary>
    [Range(1, 200, ErrorMessage = "MaxNameLength must be between 1 and 200")]
    public int MaxNameLength { get; set; } = 100;
}
