using BondCalculationService.Configuration;
using BondCalculationService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
// SERILOG CONFIGURATION
// Configured to capture EventId 9001 to Elasticsearch for regression testing
// =====================================================================
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container
builder.Services.Configure<TestDataCaptureOptions>(
    builder.Configuration.GetSection(TestDataCaptureOptions.SectionName));

builder.Services.AddScoped<IBondCalculationService, BondCalculationServiceImpl>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();
