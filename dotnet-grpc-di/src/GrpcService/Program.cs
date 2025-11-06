using GrpcService.Extensions;
using GrpcService.Services;

// Make Program accessible for integration tests
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Add gRPC services
        builder.Services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
            options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4 MB
            options.MaxSendMessageSize = 4 * 1024 * 1024; // 4 MB
        });

        // Add application services using our extension methods
        builder.Services.AddApplicationServices(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Map gRPC service
        app.MapGrpcService<GreeterService>();

        // Map health check endpoint
        app.MapHealthChecks("/health");

        // Provide a helpful message for non-gRPC requests
        app.MapGet("/", () => Results.Text(
            "Communication with gRPC endpoints must be made through a gRPC client. " +
            "To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"));

        app.Run();
    }
}
