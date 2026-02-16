using System.Text.Json.Serialization;

namespace BondCalculationService.Models;

/// <summary>
/// Represents a single test case in the golden dataset.
/// Contains only the INPUTS needed to reproduce a calculation - the expected outputs
/// are stored separately in Verify snapshots to enable easy comparison after library upgrades.
///
/// WORKFLOW:
/// 1. Test cases are captured from production via Elasticsearch logs (EventId 9001)
/// 2. The DataExporter tool curates diverse cases into this format
/// 3. xUnit tests load these cases and run them through BondCalculationService
/// 4. Verify library compares results against committed snapshots
/// 5. After library upgrade: run tests -> review diffs -> accept if correct
/// </summary>
public record BondTestCase
{
    /// <summary>
    /// Unique identifier for this test case (for tracking and debugging)
    /// </summary>
    [JsonPropertyName("testCaseId")]
    public required string TestCaseId { get; init; }

    /// <summary>
    /// Human-readable description of what this test case covers
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// The bond parameters (inputs to the calculation)
    /// </summary>
    [JsonPropertyName("bondParameters")]
    public required BondParameters BondParameters { get; init; }

    /// <summary>
    /// Market price of the bond (input for yield calculation)
    /// </summary>
    [JsonPropertyName("price")]
    public required decimal Price { get; init; }

    /// <summary>
    /// Settlement date for the calculation
    /// </summary>
    [JsonPropertyName("settlementDate")]
    public required DateOnly SettlementDate { get; init; }

    /// <summary>
    /// Source of this test case (e.g., "synthetic", "production", "edge-case")
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// Tags for categorizing test cases (e.g., ["high-yield", "short-duration"])
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// When this test case was created/captured
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
