using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace GrpcService;

public class CustomAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class CustomAuthenticationHandler : AuthenticationHandler<CustomAuthenticationOptions>
{
    public CustomAuthenticationHandler(
        IOptionsMonitor<CustomAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            // Set custom headers in the response that we want to see in gRPC trailers
            Response.Headers["x-custom-test"] = "authentication-failed";
            Response.Headers["www-authenticate"] = "Bearer realm=\"GrpcService\"";

            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
        }

        var token = authHeader.ToString();

        // Simple token validation - accept any token starting with "Bearer valid-"
        if (!token.StartsWith("Bearer valid-", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers["x-custom-test"] = "invalid-token";
            Response.Headers["www-authenticate"] = "Bearer realm=\"GrpcService\", error=\"invalid_token\"";

            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }

        // Create claims for authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.NameIdentifier, "123")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Response.Headers["x-custom-test"] = "authentication-success";

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["x-custom-test"] = "challenge-initiated";
        Response.Headers["www-authenticate"] = "Bearer realm=\"GrpcService\"";

        return base.HandleChallengeAsync(properties);
    }
}
