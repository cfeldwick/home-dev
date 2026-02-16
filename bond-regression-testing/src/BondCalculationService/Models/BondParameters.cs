using System.Text.Json.Serialization;

namespace BondCalculationService.Models;

/// <summary>
/// Represents the input parameters for a bond calculation.
/// In production, this would mirror the third-party quant library's bond representation.
/// For the POC, we use a simplified structure with common bond attributes.
/// </summary>
public record BondParameters
{
    /// <summary>
    /// Unique identifier for the bond (e.g., CUSIP, ISIN, or internal ID)
    /// </summary>
    [JsonPropertyName("cusip")]
    public required string Cusip { get; init; }

    /// <summary>
    /// Annual coupon rate as a percentage (e.g., 5.0 for 5%)
    /// </summary>
    [JsonPropertyName("couponRate")]
    public required decimal CouponRate { get; init; }

    /// <summary>
    /// Maturity date of the bond
    /// </summary>
    [JsonPropertyName("maturityDate")]
    public required DateOnly MaturityDate { get; init; }

    /// <summary>
    /// Face value of the bond (typically 100 or 1000)
    /// </summary>
    [JsonPropertyName("faceValue")]
    public decimal FaceValue { get; init; } = 100m;

    /// <summary>
    /// Number of coupon payments per year (1=annual, 2=semi-annual, 4=quarterly)
    /// </summary>
    [JsonPropertyName("frequency")]
    public int Frequency { get; init; } = 2;

    /// <summary>
    /// Day count convention (e.g., "30/360", "ACT/360", "ACT/ACT")
    /// </summary>
    [JsonPropertyName("dayCountConvention")]
    public string DayCountConvention { get; init; } = "30/360";
}
