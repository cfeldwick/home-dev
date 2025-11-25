using System.Runtime.CompilerServices;

namespace BondCalculationService.Tests;

/// <summary>
/// Module initializer for Verify configuration.
/// This runs before any tests execute to set up Verify defaults.
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// Configure Verify settings for the entire test assembly.
    ///
    /// CONFIGURATION OPTIONS:
    /// - UseDirectory: Where to store snapshot files
    /// - DontScrubDateTimes: Whether to normalize dates
    /// - AddExtraSettings: JSON serialization options
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // Configure Verify to use consistent settings across all tests
        VerifyBase.UseUniqueDirectory();

        // Don't scrub GUIDs since we're not using them in snapshots
        VerifierSettings.DontScrubGuids();

        // Use stable JSON formatting
        VerifierSettings.AddExtraSettings(settings =>
        {
            settings.WriteIndented = true;
            settings.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });
    }
}
