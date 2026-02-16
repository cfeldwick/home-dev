using System.Text.Json.Serialization;

namespace BondCalculationService.Models;

/// <summary>
/// Structured log entry format for bond calculations.
/// This is the schema used for Elasticsearch logging with EventId 9001.
/// The DataExporter tool queries Elasticsearch for these entries to build the golden dataset.
/// </summary>
public record CalculationLogEntry
{
    /// <summary>
    /// Unique correlation ID for tracing
    /// </summary>
    [JsonPropertyName("correlationId")]
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Event ID for filtering (9001 = calculation data for regression testing)
    /// </summary>
    [JsonPropertyName("eventId")]
    public int EventId { get; init; } = 9001;

    /// <summary>
    /// The operation being performed
    /// </summary>
    [JsonPropertyName("operation")]
    public required string Operation { get; init; }

    /// <summary>
    /// Input parameters for the calculation
    /// </summary>
    [JsonPropertyName("input")]
    public required CalculationInput Input { get; init; }

    /// <summary>
    /// Output/result of the calculation (null if calculation failed)
    /// </summary>
    [JsonPropertyName("output")]
    public YieldResult? Output { get; init; }

    /// <summary>
    /// Whether the calculation succeeded
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if calculation failed
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp of the calculation
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Input portion of the calculation log entry
/// </summary>
public record CalculationInput
{
    [JsonPropertyName("bondParameters")]
    public required BondParameters BondParameters { get; init; }

    [JsonPropertyName("price")]
    public required decimal Price { get; init; }

    [JsonPropertyName("settlementDate")]
    public required DateOnly SettlementDate { get; init; }
}
