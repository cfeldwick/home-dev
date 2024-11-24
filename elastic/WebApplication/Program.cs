using Serilog;
using Serilog.Events;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;
using Serilog.Sinks.Http;
using Elastic.CommonSchema.Serilog;

var ecsTextFormatterConfiguration = new EcsTextFormatterConfiguration<CustomEcsDocument>
{
    MapCustom = (ecsDocument, logEvent) =>
    {
        ecsDocument.BarclaysData = new BarclaysData {
            AppCode = "my-app-code",
            SpecialKey = "some-guid"
        };
        return ecsDocument;
    }
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
    .Enrich.WithProperty("Application", "Example")
    .Enrich.FromLogContext()
    .WriteTo.Http(
        requestUri: "http://localhost:8000",
        queueLimitBytes: null, // Optional: Adjust as needed
        textFormatter: new EcsTextFormatter<CustomEcsDocument>(ecsTextFormatterConfiguration))
    .WriteTo.Console(Formatters.CreateConsoleTextFormatter(theme: TemplateTheme.Code))
    .CreateLogger();

using var listener = new ActivityListenerConfiguration()
    .Instrument.AspNetCoreRequests()
    .TraceToSharedLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var requestObj = new {
        Name = "RequestName",
        Level1 = new {
            Level2_1 = 1,
            Level2_2 = "Hello",
        },
    };

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting up");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{   
    logger.LogWithParam("Request", requestObj, 
        logger => logger.LogInformation("Received request: {TestParam}", "TestParam1")
    );

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class CustomEcsDocument : Elastic.CommonSchema.EcsDocument
{
    public BarclaysData BarclaysData { get; set; }

    protected override void WriteAdditionalProperties(Action<string, object> write) => write("barclays", BarclaysData);
}

// Define any custom types you need
public class BarclaysData
{
    public string AppCode { get; set; }
    public string SpecialKey { get; set; }
}

public static class LoggerExtensions
{
    public static IDisposable? BeginScopeParam<TLogger>(this ILogger<TLogger> logger, string key, object value)
    {
        return logger.BeginScope(new Dictionary<string, object> {
            [$"@{key}"] = value
        });
    }

    public static void LogWithParam<T> (this ILogger<T> logger, string key, object value, Action<ILogger<T>> logDelegate)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { [$"@{key}"] = value });
        logDelegate(logger);
    }
}