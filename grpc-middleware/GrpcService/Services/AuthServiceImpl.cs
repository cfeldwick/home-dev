using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace GrpcService.Services;

/// <summary>
/// Implementation of the AuthService gRPC service.
/// This service requires authentication for all methods.
/// </summary>
[Authorize]
public class AuthServiceImpl : AuthService.AuthServiceBase
{
    private readonly ILogger<AuthServiceImpl> _logger;

    public AuthServiceImpl(ILogger<AuthServiceImpl> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets user information. Requires authentication.
    /// If authentication fails, the custom headers set by the authentication handler
    /// will be available in the gRPC trailing metadata (thanks to HeaderToTrailerMiddleware).
    /// </summary>
    public override Task<GetUserInfoResponse> GetUserInfo(
        GetUserInfoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("GetUserInfo called for user_id: {UserId}", request.UserId);

        // Get user information from claims
        var userName = context.GetHttpContext().User.Identity?.Name ?? "unknown";
        var userId = context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? request.UserId;

        var response = new GetUserInfoResponse
        {
            UserId = userId,
            Username = userName,
            Email = $"{userName}@example.com"
        };

        return Task.FromResult(response);
    }
}
