using System.Text.Json;
using BondCalculationService.Models;
using BondCalculationService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BondCalculationService.Tests;

/// <summary>
/// Regression tests for bond yield calculations using snapshot testing.
///
/// WORKFLOW OVERVIEW:
/// ==================
/// 1. INITIAL SETUP: Run tests with no snapshots -> Verify creates them
/// 2. NORMAL DEVELOPMENT: Tests compare results against committed snapshots
/// 3. LIBRARY UPGRADE: If quant library changes, tests may fail
/// 4. REVIEW CHANGES: Use diff tool to review what changed
/// 5. ACCEPT/REJECT: If changes are expected, accept new snapshots
///
/// SNAPSHOT LOCATION:
/// - Snapshots are stored in tests/Snapshots/ directory
/// - Each test case gets its own .verified.txt file
/// - File naming: {TestClass}.{TestMethod}_{CaseId}.verified.txt
///
/// HOW IT WORKS:
/// - Verify library serializes YieldResult to JSON
/// - On first run, creates .verified.txt snapshot
/// - On subsequent runs, creates .received.txt and compares
/// - If different, test fails and shows diff
/// - Developer reviews and either fixes code or accepts new snapshot
/// </summary>
[UsesVerify]
public class BondYieldRegressionTests
{
    private readonly IBondCalculationService _calculationService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BondYieldRegressionTests()
    {
        // Using NullLogger for tests - we don't need to capture logs here
        // In production, the service logs to Elasticsearch
        _calculationService = new BondCalculationServiceImpl(
            NullLogger<BondCalculationServiceImpl>.Instance);
    }

    /// <summary>
    /// Loads synthetic test cases from the golden dataset.
    /// These are hand-crafted cases covering various bond types.
    /// </summary>
    public static IEnumerable<object[]> SyntheticTestCases()
    {
        var json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "GoldenDataset", "synthetic-cases.json"));
        var testCases = JsonSerializer.Deserialize<List<BondTestCase>>(json, JsonOptions)!;

        foreach (var testCase in testCases)
        {
            yield return new object[] { testCase };
        }
    }

    /// <summary>
    /// Loads production test cases from the golden dataset.
    /// These are real (anonymized) cases captured from Elasticsearch logs.
    /// </summary>
    public static IEnumerable<object[]> ProductionTestCases()
    {
        var json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "GoldenDataset", "production-cases.json"));
        var testCases = JsonSerializer.Deserialize<List<BondTestCase>>(json, JsonOptions)!;

        foreach (var testCase in testCases)
        {
            yield return new object[] { testCase };
        }
    }

    /// <summary>
    /// Regression test for synthetic test cases.
    ///
    /// USAGE:
    /// - First run: Creates snapshot files in Snapshots/ directory
    /// - Subsequent runs: Compares against snapshots
    /// - After library upgrade: Review diffs, then run:
    ///   dotnet verify accept --all
    /// </summary>
    [Theory]
    [MemberData(nameof(SyntheticTestCases))]
    public async Task CalculateYield_SyntheticCases_MatchesSnapshot(BondTestCase testCase)
    {
        // Arrange - test case provides all inputs
        // Act - perform the calculation
        var result = _calculationService.CalculateYield(
            testCase.BondParameters,
            testCase.Price,
            testCase.SettlementDate);

        // Assert using Verify snapshot testing
        // Creates a stable, comparable representation excluding volatile fields
        var snapshot = CreateSnapshotObject(testCase, result);

        // UseParameters adds the testCaseId to the snapshot filename
        // This creates files like: BondYieldRegressionTests.CalculateYield_SyntheticCases_MatchesSnapshot_SYN-001.verified.txt
        await Verify(snapshot)
            .UseDirectory("Snapshots")
            .UseParameters(testCase.TestCaseId);
    }

    /// <summary>
    /// Regression test for production test cases.
    ///
    /// These cases come from real production data captured via Elasticsearch.
    /// They are anonymized by the DataExporter tool before committing.
    ///
    /// IMPORTANT: Production cases are especially valuable because they
    /// represent real-world usage patterns that synthetic cases may miss.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProductionTestCases))]
    public async Task CalculateYield_ProductionCases_MatchesSnapshot(BondTestCase testCase)
    {
        // Arrange - test case provides all inputs
        // Act - perform the calculation
        var result = _calculationService.CalculateYield(
            testCase.BondParameters,
            testCase.Price,
            testCase.SettlementDate);

        // Assert using Verify snapshot testing
        var snapshot = CreateSnapshotObject(testCase, result);

        await Verify(snapshot)
            .UseDirectory("Snapshots")
            .UseParameters(testCase.TestCaseId);
    }

    /// <summary>
    /// Creates a snapshot-friendly object from the test case and result.
    ///
    /// IMPORTANT: We exclude volatile fields like CalculatedAt and EngineVersion
    /// from the core comparison. EngineVersion is tracked separately so we know
    /// when the library version changes but it doesn't cause false test failures.
    /// </summary>
    private static object CreateSnapshotObject(BondTestCase testCase, YieldResult result)
    {
        return new
        {
            // Include test case metadata for traceability
            TestCaseId = testCase.TestCaseId,
            Description = testCase.Description,

            // Include inputs (so snapshot is self-documenting)
            Inputs = new
            {
                Cusip = testCase.BondParameters.Cusip,
                CouponRate = testCase.BondParameters.CouponRate,
                MaturityDate = testCase.BondParameters.MaturityDate.ToString("yyyy-MM-dd"),
                FaceValue = testCase.BondParameters.FaceValue,
                Frequency = testCase.BondParameters.Frequency,
                DayCountConvention = testCase.BondParameters.DayCountConvention,
                Price = testCase.Price,
                SettlementDate = testCase.SettlementDate.ToString("yyyy-MM-dd")
            },

            // The actual results we're regression testing
            // NOTE: CalculatedAt is excluded because it changes every run
            Results = new
            {
                YieldToMaturity = result.YieldToMaturity,
                ModifiedDuration = result.ModifiedDuration,
                MacaulayDuration = result.MacaulayDuration,
                Convexity = result.Convexity,
                AccruedInterest = result.AccruedInterest,
                CleanPrice = result.CleanPrice,
                DirtyPrice = result.DirtyPrice
            },

            // Track engine version separately for informational purposes
            // This helps identify when library upgrades cause changes
            EngineVersion = result.EngineVersion
        };
    }

    /// <summary>
    /// Quick sanity check that calculations produce reasonable results.
    /// This is a traditional unit test, not a snapshot test.
    /// </summary>
    [Fact]
    public void CalculateYield_ParBond_YieldEqualsApproximatelyCouponRate()
    {
        // Arrange - a bond trading at par should have yield ≈ coupon rate
        var bond = new BondParameters
        {
            Cusip = "TEST00001",
            CouponRate = 5.0m,
            MaturityDate = new DateOnly(2029, 6, 15),
            FaceValue = 100m,
            Frequency = 2
        };

        // Act
        var result = _calculationService.CalculateYield(
            bond,
            price: 100m,
            settlementDate: new DateOnly(2024, 6, 15));

        // Assert - yield should be close to coupon rate for par bond
        result.YieldToMaturity.Should().BeApproximately(5.0m, 0.5m,
            "a bond trading at par should have yield approximately equal to coupon rate");
    }

    /// <summary>
    /// Verify that premium bonds have lower yield than coupon rate.
    /// </summary>
    [Fact]
    public void CalculateYield_PremiumBond_YieldLessThanCouponRate()
    {
        // Arrange - premium bond (price > face value)
        var bond = new BondParameters
        {
            Cusip = "TEST00002",
            CouponRate = 6.0m,
            MaturityDate = new DateOnly(2029, 6, 15),
            FaceValue = 100m,
            Frequency = 2
        };

        // Act
        var result = _calculationService.CalculateYield(
            bond,
            price: 110m, // Premium price
            settlementDate: new DateOnly(2024, 6, 15));

        // Assert
        result.YieldToMaturity.Should().BeLessThan(bond.CouponRate,
            "a premium bond should have yield less than coupon rate");
    }

    /// <summary>
    /// Verify that discount bonds have higher yield than coupon rate.
    /// </summary>
    [Fact]
    public void CalculateYield_DiscountBond_YieldGreaterThanCouponRate()
    {
        // Arrange - discount bond (price < face value)
        var bond = new BondParameters
        {
            Cusip = "TEST00003",
            CouponRate = 4.0m,
            MaturityDate = new DateOnly(2029, 6, 15),
            FaceValue = 100m,
            Frequency = 2
        };

        // Act
        var result = _calculationService.CalculateYield(
            bond,
            price: 90m, // Discount price
            settlementDate: new DateOnly(2024, 6, 15));

        // Assert
        result.YieldToMaturity.Should().BeGreaterThan(bond.CouponRate,
            "a discount bond should have yield greater than coupon rate");
    }
}
