using System.Text.Json.Serialization;

namespace BondCalculationService.Models;

/// <summary>
/// Represents the output of a bond yield calculation.
/// Contains yield-to-maturity and related analytics that the quant library would return.
/// </summary>
public record YieldResult
{
    /// <summary>
    /// Yield to Maturity as a percentage (e.g., 4.5 for 4.5%)
    /// </summary>
    [JsonPropertyName("yieldToMaturity")]
    public required decimal YieldToMaturity { get; init; }

    /// <summary>
    /// Modified duration - sensitivity of bond price to yield changes
    /// </summary>
    [JsonPropertyName("modifiedDuration")]
    public required decimal ModifiedDuration { get; init; }

    /// <summary>
    /// Macaulay duration - weighted average time to receive cash flows
    /// </summary>
    [JsonPropertyName("macaulayDuration")]
    public required decimal MacaulayDuration { get; init; }

    /// <summary>
    /// Convexity - rate of change of duration
    /// </summary>
    [JsonPropertyName("convexity")]
    public required decimal Convexity { get; init; }

    /// <summary>
    /// Accrued interest since last coupon payment
    /// </summary>
    [JsonPropertyName("accruedInterest")]
    public required decimal AccruedInterest { get; init; }

    /// <summary>
    /// Clean price (excluding accrued interest)
    /// </summary>
    [JsonPropertyName("cleanPrice")]
    public required decimal CleanPrice { get; init; }

    /// <summary>
    /// Dirty price (including accrued interest)
    /// </summary>
    [JsonPropertyName("dirtyPrice")]
    public required decimal DirtyPrice { get; init; }

    /// <summary>
    /// Timestamp when calculation was performed
    /// </summary>
    [JsonPropertyName("calculatedAt")]
    public required DateTime CalculatedAt { get; init; }

    /// <summary>
    /// Version of the calculation engine/library used
    /// This is critical for regression testing - changes here trigger new snapshots
    /// </summary>
    [JsonPropertyName("engineVersion")]
    public required string EngineVersion { get; init; }
}
