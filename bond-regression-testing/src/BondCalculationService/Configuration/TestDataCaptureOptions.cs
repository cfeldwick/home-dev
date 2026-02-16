namespace BondCalculationService.Configuration;

/// <summary>
/// Configuration options for test data capture.
/// Controls whether calculation data is sent to Elasticsearch for regression testing.
/// </summary>
public class TestDataCaptureOptions
{
    public const string SectionName = "TestDataCapture";

    /// <summary>
    /// Feature flag to enable/disable test data capture.
    /// Should be enabled in production to capture data, disabled in test environments.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// EventId used for filtering calculation logs.
    /// Default is 9001 - the Elasticsearch sink is configured to only accept this EventId.
    /// </summary>
    public int EventId { get; set; } = 9001;

    /// <summary>
    /// Sampling rate (0.0 to 1.0) - what percentage of calculations to log.
    /// 1.0 = log everything, 0.1 = log 10% of calculations.
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;
}
