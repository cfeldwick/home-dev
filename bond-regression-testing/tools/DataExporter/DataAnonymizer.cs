using BondCalculationService.Models;

namespace DataExporter;

/// <summary>
/// Anonymizes production data before it's committed to the golden dataset.
///
/// ANONYMIZATION RULES:
/// - CUSIPs are replaced with sequential anonymized identifiers
/// - Settlement dates may be shifted if needed for privacy
/// - No PII should exist in bond data, but this is a safety layer
///
/// WHY ANONYMIZE:
/// - Production CUSIPs may be considered proprietary trading information
/// - Specific settlement dates could reveal trading patterns
/// - General good practice for any production data in version control
/// </summary>
public class DataAnonymizer
{
    private int _anonymousCounter = 1;

    /// <summary>
    /// Convert a calculation log entry into an anonymized test case.
    /// </summary>
    /// <param name="entry">The original log entry from Elasticsearch</param>
    /// <param name="testCaseId">The ID to assign to this test case</param>
    /// <returns>Anonymized test case ready for the golden dataset</returns>
    public BondTestCase AnonymizeToTestCase(CalculationLogEntry entry, string testCaseId)
    {
        var input = entry.Input;

        // Generate anonymized CUSIP
        var anonymizedCusip = $"ANON{_anonymousCounter:D6}";
        _anonymousCounter++;

        // Create description based on bond characteristics
        var description = GenerateDescription(input);

        // Generate tags based on characteristics
        var tags = GenerateTags(input);

        return new BondTestCase
        {
            TestCaseId = testCaseId,
            Description = description,
            BondParameters = new BondParameters
            {
                Cusip = anonymizedCusip,
                CouponRate = input.BondParameters.CouponRate,
                MaturityDate = input.BondParameters.MaturityDate,
                FaceValue = input.BondParameters.FaceValue,
                Frequency = input.BondParameters.Frequency,
                DayCountConvention = input.BondParameters.DayCountConvention
            },
            Price = input.Price,
            SettlementDate = input.SettlementDate,
            Source = "production",
            Tags = tags,
            CreatedAt = entry.Timestamp
        };
    }

    /// <summary>
    /// Generate a human-readable description based on bond characteristics.
    /// </summary>
    private static string GenerateDescription(CalculationInput input)
    {
        var priceType = input.Price switch
        {
            < 98 => "discount",
            < 102 => "par",
            _ => "premium"
        };

        var yieldType = input.BondParameters.CouponRate switch
        {
            < 4 => "low-yield",
            < 6 => "medium-yield",
            _ => "high-yield"
        };

        var yearsToMaturity = (input.BondParameters.MaturityDate.DayNumber - input.SettlementDate.DayNumber) / 365.0;
        var durationDesc = yearsToMaturity switch
        {
            < 2 => "short-term",
            < 5 => "medium-term",
            < 10 => "intermediate",
            _ => "long-term"
        };

        return $"Production case - {durationDesc} {yieldType} bond at {priceType} (anonymized)";
    }

    /// <summary>
    /// Generate tags for categorizing the test case.
    /// </summary>
    private static List<string> GenerateTags(CalculationInput input)
    {
        var tags = new List<string> { "anonymized" };

        // Price tags
        if (input.Price < 90) tags.Add("deep-discount");
        else if (input.Price < 98) tags.Add("discount");
        else if (input.Price > 110) tags.Add("deep-premium");
        else if (input.Price > 102) tags.Add("premium");
        else tags.Add("near-par");

        // Coupon tags
        if (input.BondParameters.CouponRate < 3) tags.Add("low-coupon");
        else if (input.BondParameters.CouponRate > 7) tags.Add("high-coupon");

        // Duration tags
        var yearsToMaturity = (input.BondParameters.MaturityDate.DayNumber - input.SettlementDate.DayNumber) / 365.0;
        if (yearsToMaturity < 2) tags.Add("short-duration");
        else if (yearsToMaturity > 10) tags.Add("long-duration");

        // Day count convention
        if (input.BondParameters.DayCountConvention.Contains("ACT"))
            tags.Add("actual-daycount");

        return tags;
    }
}
