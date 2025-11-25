using BondCalculationService.Models;
using Microsoft.Extensions.Logging;

namespace BondCalculationService.Services;

/// <summary>
/// Bond calculation service that wraps the third-party quant library.
/// For this POC, we implement simplified but realistic calculation logic.
///
/// REGRESSION TESTING WORKFLOW:
/// 1. This service logs all calculation inputs/outputs with EventId 9001
/// 2. Serilog is configured to send EventId 9001 to Elasticsearch
/// 3. The DataExporter tool queries Elasticsearch to build golden datasets
/// 4. Tests use Verify to compare new results against committed snapshots
/// 5. After library upgrade: if results differ, developer reviews and accepts
/// </summary>
public class BondCalculationServiceImpl : IBondCalculationService
{
    private readonly ILogger<BondCalculationServiceImpl> _logger;

    // Simulates the version of the third-party quant library
    // When this changes, we expect regression test results may differ
    private const string EngineVersion = "QuantLib-POC-1.0.0";

    // EventId for regression test data capture
    // Only logs with this EventId are sent to the Elasticsearch sink for test data
    private static readonly EventId RegressionTestDataEventId = new(9001, "BondCalculationData");

    public BondCalculationServiceImpl(ILogger<BondCalculationServiceImpl> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate yield-to-maturity and related bond analytics.
    ///
    /// NOTE: This is simplified POC logic, not financially accurate!
    /// In production, this would call into QuantLib or similar library.
    /// </summary>
    public YieldResult CalculateYield(BondParameters bond, decimal price, DateOnly settlementDate)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var timestamp = DateTime.UtcNow;

        try
        {
            // =====================================================================
            // SIMPLIFIED YIELD CALCULATION (POC ONLY)
            // Real implementation would use Newton-Raphson iteration or similar
            // =====================================================================

            // Calculate time to maturity in years
            var yearsToMaturity = CalculateYearsToMaturity(settlementDate, bond.MaturityDate);

            // Simplified yield approximation using the bond pricing formula
            // YTM ≈ (Coupon + (FaceValue - Price) / Years) / ((FaceValue + Price) / 2)
            var annualCoupon = bond.FaceValue * (bond.CouponRate / 100m);
            var capitalGainPerYear = (bond.FaceValue - price) / (decimal)yearsToMaturity;
            var averagePrice = (bond.FaceValue + price) / 2m;
            var yieldToMaturity = ((annualCoupon + capitalGainPerYear) / averagePrice) * 100m;

            // Calculate accrued interest (simplified 30/360 calculation)
            var accruedInterest = CalculateAccruedInterest(bond, settlementDate);

            // Calculate duration (simplified Macaulay duration)
            var macaulayDuration = CalculateMacaulayDuration(bond, yieldToMaturity, yearsToMaturity);
            var modifiedDuration = macaulayDuration / (1m + (yieldToMaturity / 100m) / bond.Frequency);

            // Calculate convexity (simplified)
            var convexity = CalculateConvexity(yearsToMaturity, bond.Frequency);

            var result = new YieldResult
            {
                YieldToMaturity = Math.Round(yieldToMaturity, 6),
                ModifiedDuration = Math.Round(modifiedDuration, 6),
                MacaulayDuration = Math.Round(macaulayDuration, 6),
                Convexity = Math.Round(convexity, 6),
                AccruedInterest = Math.Round(accruedInterest, 6),
                CleanPrice = Math.Round(price, 6),
                DirtyPrice = Math.Round(price + accruedInterest, 6),
                CalculatedAt = timestamp,
                EngineVersion = EngineVersion
            };

            // =====================================================================
            // STRUCTURED LOGGING FOR REGRESSION TEST DATA CAPTURE
            // This log entry is captured by Elasticsearch for later export
            // EventId 9001 is filtered by Serilog configuration
            // =====================================================================
            _logger.Log(
                LogLevel.Information,
                RegressionTestDataEventId,
                "Bond calculation completed: {CorrelationId} {Operation} {Input} {Output} {Success}",
                correlationId,
                "CalculateYield",
                new CalculationInput
                {
                    BondParameters = bond,
                    Price = price,
                    SettlementDate = settlementDate
                },
                result,
                true
            );

            return result;
        }
        catch (Exception ex)
        {
            // Log failed calculations too - they may reveal edge cases
            _logger.Log(
                LogLevel.Warning,
                RegressionTestDataEventId,
                ex,
                "Bond calculation failed: {CorrelationId} {Operation} {Input} {Success} {ErrorMessage}",
                correlationId,
                "CalculateYield",
                new CalculationInput
                {
                    BondParameters = bond,
                    Price = price,
                    SettlementDate = settlementDate
                },
                false,
                ex.Message
            );

            throw;
        }
    }

    private static double CalculateYearsToMaturity(DateOnly settlementDate, DateOnly maturityDate)
    {
        var days = maturityDate.DayNumber - settlementDate.DayNumber;
        return days / 365.25; // Simplified - real impl uses day count convention
    }

    private static decimal CalculateAccruedInterest(BondParameters bond, DateOnly settlementDate)
    {
        // Simplified: assume we're halfway through a coupon period
        // Real implementation would calculate from last coupon date
        var periodicCoupon = bond.FaceValue * (bond.CouponRate / 100m) / bond.Frequency;
        var daysSinceLastCoupon = settlementDate.Day; // Very simplified!
        var daysInPeriod = 180; // 30/360 convention
        return periodicCoupon * (daysSinceLastCoupon / (decimal)daysInPeriod);
    }

    private static decimal CalculateMacaulayDuration(BondParameters bond, decimal yieldToMaturity, double yearsToMaturity)
    {
        // Simplified duration calculation
        // Real implementation would sum PV-weighted cash flow times
        var couponContribution = (1m - 1m / (decimal)Math.Pow(1 + (double)(yieldToMaturity / 100m) / bond.Frequency,
            yearsToMaturity * bond.Frequency)) / (yieldToMaturity / 100m);

        var principalContribution = (decimal)yearsToMaturity /
            (decimal)Math.Pow(1 + (double)(yieldToMaturity / 100m) / bond.Frequency, yearsToMaturity * bond.Frequency);

        return couponContribution + principalContribution;
    }

    private static decimal CalculateConvexity(double yearsToMaturity, int frequency)
    {
        // Very simplified convexity approximation
        return (decimal)(yearsToMaturity * yearsToMaturity / frequency);
    }
}
