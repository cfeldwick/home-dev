using GrpcService;
using GrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc();

// Add gRPC JSON transcoding
builder.Services.AddGrpcJsonTranscoding();

// Add gRPC reflection (useful for testing with tools like grpcurl)
builder.Services.AddGrpcReflection();

// Add custom authentication
builder.Services.AddAuthentication("CustomScheme")
    .AddScheme<CustomAuthenticationOptions, CustomAuthenticationHandler>(
        "CustomScheme",
        options => { });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline

// 1. Authentication - this sets headers in Response.Headers
app.UseAuthentication();

// 2. HeaderToTrailerMiddleware - MUST come after authentication and before authorization
//    This copies specified headers from Response.Headers to gRPC trailers
app.UseHeaderToTrailer("www-authenticate", "x-custom-test");

// 3. Authorization
app.UseAuthorization();

// Map gRPC services
app.MapGrpcService<AuthServiceImpl>();

// Map gRPC reflection service
app.MapGrpcReflectionService();

// Add a simple endpoint to verify the server is running
app.MapGet("/", () => "gRPC service is running. Use a gRPC client to interact with it.");

app.Run();
