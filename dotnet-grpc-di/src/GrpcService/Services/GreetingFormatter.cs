using GrpcService.Options;
using Microsoft.Extensions.Options;

namespace GrpcService.Services;

/// <summary>
/// Default implementation of <see cref="IGreetingFormatter"/>.
/// </summary>
public sealed class GreetingFormatter : IGreetingFormatter
{
    private readonly GreeterOptions _options;
    private readonly ILogger<GreetingFormatter> _logger;

    public GreetingFormatter(
        IOptions<GreeterOptions> options,
        ILogger<GreetingFormatter> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string FormatGreeting(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Attempted to format greeting with null or empty name");
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        }

        if (name.Length > _options.MaxNameLength)
        {
            _logger.LogWarning(
                "Name length {Length} exceeds maximum {MaxLength}",
                name.Length,
                _options.MaxNameLength);

            name = name[.._options.MaxNameLength];
        }

        var greeting = $"{_options.GreetingPrefix}, {name}!";

        _logger.LogDebug("Formatted greeting: {Greeting}", greeting);

        return greeting;
    }
}
