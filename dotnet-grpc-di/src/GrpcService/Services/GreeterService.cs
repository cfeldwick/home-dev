using Grpc.Core;
using GrpcService.Options;
using Microsoft.Extensions.Options;

namespace GrpcService.Services;

/// <summary>
/// gRPC service implementation for the Greeter service.
/// Demonstrates dependency injection of options and custom services.
/// </summary>
public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly IGreetingFormatter _greetingFormatter;
    private readonly ITimestampProvider _timestampProvider;
    private readonly IExternalServiceClient _externalServiceClient;
    private readonly GreeterOptions _options;

    public GreeterService(
        ILogger<GreeterService> logger,
        IGreetingFormatter greetingFormatter,
        ITimestampProvider timestampProvider,
        IExternalServiceClient externalServiceClient,
        IOptions<GreeterOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _greetingFormatter = greetingFormatter ?? throw new ArgumentNullException(nameof(greetingFormatter));
        _timestampProvider = timestampProvider ?? throw new ArgumentNullException(nameof(timestampProvider));
        _externalServiceClient = externalServiceClient ?? throw new ArgumentNullException(nameof(externalServiceClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received SayHello request for: {Name}", request.Name);

        // Try to get nickname from external service
        var nickname = await _externalServiceClient.GetNicknameAsync(request.Name, context.CancellationToken);

        // Use nickname if found, otherwise use the original name
        var nameToGreet = nickname ?? request.Name;

        if (nickname != null)
        {
            _logger.LogInformation("Using nickname {Nickname} for {Name}", nickname, request.Name);
        }

        var message = _greetingFormatter.FormatGreeting(nameToGreet);

        var reply = new HelloReply
        {
            Message = message,
            Timestamp = _timestampProvider.GetCurrentTimestamp(),
            Version = _options.AppVersion
        };

        _logger.LogDebug("Sending reply: {Message}", reply.Message);

        return reply;
    }

    public override async Task<HelloReply> SayHelloWithMetadata(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received SayHelloWithMetadata request for: {Name}", request.Name);

        // Try to get nickname from external service
        var nickname = await _externalServiceClient.GetNicknameAsync(request.Name, context.CancellationToken);

        // Use nickname if found, otherwise use the original name
        var nameToGreet = nickname ?? request.Name;

        if (nickname != null)
        {
            _logger.LogInformation("Using nickname {Nickname} for {Name}", nickname, request.Name);
        }

        var message = _greetingFormatter.FormatGreeting(nameToGreet);

        var reply = new HelloReply
        {
            Message = message
        };

        // Include metadata only if configured
        if (_options.IncludeMetadata)
        {
            reply.Timestamp = _timestampProvider.GetCurrentTimestamp();
            reply.Version = _options.AppVersion;

            // Add response headers
            var headers = new Metadata
            {
                { "x-greeting-timestamp", reply.Timestamp },
                { "x-app-version", reply.Version }
            };

            context.ResponseTrailers.Add(headers[0]);
            context.ResponseTrailers.Add(headers[1]);
        }

        _logger.LogDebug(
            "Sending reply with metadata: {Message} (metadata included: {IncludeMetadata})",
            reply.Message,
            _options.IncludeMetadata);

        return reply;
    }
}
