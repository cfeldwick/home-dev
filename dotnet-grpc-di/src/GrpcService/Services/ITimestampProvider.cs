namespace GrpcService.Services;

/// <summary>
/// Service for providing timestamps.
/// </summary>
public interface ITimestampProvider
{
    /// <summary>
    /// Gets the current timestamp in ISO 8601 format.
    /// </summary>
    /// <returns>The current timestamp as a string.</returns>
    string GetCurrentTimestamp();
}
