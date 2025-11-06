using GrpcService.Services;

namespace GrpcService.IntegrationTests.Fakes;

/// <summary>
/// Fake implementation of IExternalServiceClient for integration testing.
/// Demonstrates the test double pattern for replacing external dependencies.
/// </summary>
public sealed class FakeExternalServiceClient : IExternalServiceClient
{
    private readonly Dictionary<string, string> _nicknameDatabase;
    private bool _isHealthy = true;

    public FakeExternalServiceClient()
    {
        // Pre-populate with some test data
        _nicknameDatabase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["William"] = "Bill",
            ["Robert"] = "Bob",
            ["Richard"] = "Dick",
            ["James"] = "Jim",
            ["Michael"] = "Mike",
            ["Elizabeth"] = "Liz",
            ["Jennifer"] = "Jenny",
            ["Christopher"] = "Chris",
            ["Matthew"] = "Matt",
            ["Alexander"] = "Alex"
        };
    }

    /// <summary>
    /// Adds a nickname mapping to the fake database.
    /// Useful for setting up test-specific data.
    /// </summary>
    public void AddNickname(string name, string nickname)
    {
        _nicknameDatabase[name] = nickname;
    }

    /// <summary>
    /// Sets whether the health check should return healthy or unhealthy.
    /// Useful for testing failure scenarios.
    /// </summary>
    public void SetHealthy(bool isHealthy)
    {
        _isHealthy = isHealthy;
    }

    /// <summary>
    /// Clears all nickname mappings from the fake database.
    /// </summary>
    public void ClearNicknames()
    {
        _nicknameDatabase.Clear();
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        // Simulate a small delay to make it more realistic
        return Task.FromResult(_isHealthy);
    }

    public Task<string?> GetNicknameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult<string?>(null);
        }

        // Lookup in our fake database
        if (_nicknameDatabase.TryGetValue(name, out var nickname))
        {
            return Task.FromResult<string?>(nickname);
        }

        // No nickname found
        return Task.FromResult<string?>(null);
    }
}
