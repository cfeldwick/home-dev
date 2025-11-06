namespace GrpcService.Services;

/// <summary>
/// Provides UTC timestamps in ISO 8601 format.
/// </summary>
public sealed class UtcTimestampProvider : ITimestampProvider
{
    public string GetCurrentTimestamp()
    {
        return DateTime.UtcNow.ToString("O"); // ISO 8601 format
    }
}
