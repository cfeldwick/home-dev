namespace GrpcService;

/// <summary>
/// Middleware that copies specified HTTP response headers to gRPC response trailers.
/// This is necessary because some authentication handlers set headers in HttpContext.Response.Headers
/// which are not accessible to gRPC clients unless copied to trailers.
/// </summary>
public class HeaderToTrailerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HeaderToTrailerMiddleware> _logger;
    private readonly HashSet<string> _headersToCapture;

    public HeaderToTrailerMiddleware(
        RequestDelegate next,
        ILogger<HeaderToTrailerMiddleware> logger,
        IEnumerable<string> headersToCapture)
    {
        _next = next;
        _logger = logger;
        _headersToCapture = new HashSet<string>(
            headersToCapture,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process gRPC requests
        if (context.Request.ContentType?.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Hook into the response to capture headers before they're sent
            context.Response.OnStarting(() =>
            {
                // Copy specified headers from Response.Headers to gRPC trailers
                foreach (var headerName in _headersToCapture)
                {
                    if (context.Response.Headers.TryGetValue(headerName, out var headerValue))
                    {
                        // Add to gRPC trailers
                        context.Response.AppendTrailer(headerName, headerValue.ToString());

                        _logger.LogDebug(
                            "Copied header '{HeaderName}' with value '{HeaderValue}' to gRPC trailers",
                            headerName,
                            headerValue);
                    }
                }

                return Task.CompletedTask;
            });
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering HeaderToTrailerMiddleware
/// </summary>
public static class HeaderToTrailerMiddlewareExtensions
{
    /// <summary>
    /// Adds HeaderToTrailerMiddleware to the application pipeline.
    /// Should be called after UseAuthentication() and before UseAuthorization().
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="headersToCapture">List of header names to copy to trailers</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseHeaderToTrailer(
        this IApplicationBuilder builder,
        params string[] headersToCapture)
    {
        if (headersToCapture == null || headersToCapture.Length == 0)
        {
            headersToCapture = new[] { "www-authenticate", "x-custom-test" };
        }

        return builder.UseMiddleware<HeaderToTrailerMiddleware>(headersToCapture);
    }
}
