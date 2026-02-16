using BondCalculationService.Models;

namespace DataExporter;

/// <summary>
/// Curates diverse test cases from production data.
///
/// CURATION STRATEGY:
/// The goal is to select a representative sample that covers:
/// - Different bond types (corporate, treasury, high-yield)
/// - Different price ranges (discount, par, premium)
/// - Different durations (short, medium, long)
/// - Edge cases (near maturity, very high/low coupons)
///
/// This ensures the golden dataset catches regressions across the full
/// range of calculation scenarios.
/// </summary>
public class TestCaseCurator
{
    /// <summary>
    /// Maximum number of test cases to include in the golden dataset.
    /// Too many cases slow down tests; too few miss edge cases.
    /// </summary>
    private const int MaxTestCases = 50;

    /// <summary>
    /// Curate a diverse set of test cases from the raw log entries.
    /// </summary>
    public List<CalculationLogEntry> CurateTestCases(List<CalculationLogEntry> entries)
    {
        if (entries.Count <= MaxTestCases)
        {
            Console.WriteLine($"       All {entries.Count} entries selected (under limit)");
            return entries;
        }

        var selected = new List<CalculationLogEntry>();

        // Strategy 1: Ensure we have cases from different price buckets
        var priceBuckets = entries
            .GroupBy(e => GetPriceBucket(e.Input.Price))
            .ToDictionary(g => g.Key, g => g.ToList());

        Console.WriteLine($"       Price buckets found: {string.Join(", ", priceBuckets.Keys)}");

        // Select proportionally from each bucket
        foreach (var bucket in priceBuckets)
        {
            var casesFromBucket = Math.Max(1, MaxTestCases / priceBuckets.Count);
            selected.AddRange(bucket.Value.Take(casesFromBucket));
        }

        // Strategy 2: Ensure we have cases with different coupon rates
        var couponBuckets = entries
            .Where(e => !selected.Contains(e))
            .GroupBy(e => GetCouponBucket(e.Input.BondParameters.CouponRate))
            .ToDictionary(g => g.Key, g => g.ToList());

        Console.WriteLine($"       Coupon buckets found: {string.Join(", ", couponBuckets.Keys)}");

        foreach (var bucket in couponBuckets)
        {
            if (selected.Count >= MaxTestCases) break;
            var toAdd = bucket.Value.FirstOrDefault();
            if (toAdd != null && !selected.Contains(toAdd))
            {
                selected.Add(toAdd);
            }
        }

        // Strategy 3: Ensure we have different maturity ranges
        var maturityBuckets = entries
            .Where(e => !selected.Contains(e))
            .GroupBy(e => GetMaturityBucket(e.Input.BondParameters.MaturityDate, e.Input.SettlementDate))
            .ToDictionary(g => g.Key, g => g.ToList());

        Console.WriteLine($"       Maturity buckets found: {string.Join(", ", maturityBuckets.Keys)}");

        foreach (var bucket in maturityBuckets)
        {
            if (selected.Count >= MaxTestCases) break;
            var toAdd = bucket.Value.FirstOrDefault();
            if (toAdd != null && !selected.Contains(toAdd))
            {
                selected.Add(toAdd);
            }
        }

        return selected.Take(MaxTestCases).ToList();
    }

    private static string GetPriceBucket(decimal price) => price switch
    {
        < 90 => "deep-discount",
        < 98 => "discount",
        < 102 => "par",
        < 110 => "premium",
        _ => "deep-premium"
    };

    private static string GetCouponBucket(decimal couponRate) => couponRate switch
    {
        < 2 => "very-low",
        < 4 => "low",
        < 6 => "medium",
        < 8 => "high",
        _ => "very-high"
    };

    private static string GetMaturityBucket(DateOnly maturity, DateOnly settlement)
    {
        var yearsToMaturity = (maturity.DayNumber - settlement.DayNumber) / 365.0;
        return yearsToMaturity switch
        {
            < 1 => "very-short",
            < 3 => "short",
            < 7 => "medium",
            < 15 => "long",
            _ => "very-long"
        };
    }
}
