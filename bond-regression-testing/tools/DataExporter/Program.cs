using System.Text.Json;
using BondCalculationService.Models;
using DataExporter;

/// <summary>
/// Data Export Tool for Bond Regression Testing
///
/// PURPOSE:
/// This tool curates production calculation data for use in regression testing.
/// It reads from Elasticsearch (or mock data for POC) and creates the golden dataset.
///
/// WORKFLOW:
/// 1. Query Elasticsearch for recent bond calculations (EventId 9001)
/// 2. Apply sampling logic to select diverse test cases
/// 3. Anonymize sensitive data (CUSIP -> anonymized identifier)
/// 4. Output to tests/GoldenDataset/production-cases.json
///
/// USAGE:
/// dotnet run --project tools/DataExporter
/// dotnet run --project tools/DataExporter -- --output ../tests/BondCalculationService.Tests/GoldenDataset/production-cases.json
/// </summary>

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Bond Calculation Data Exporter                            ║");
Console.WriteLine("║  Curates production data for regression testing            ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Parse command line arguments
var outputPath = args.Length > 1 && args[0] == "--output"
    ? args[1]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "tests", "BondCalculationService.Tests", "GoldenDataset", "production-cases.json");

// For POC: Read from mock Elasticsearch data file
// In production: This would query Elasticsearch with EventId 9001 filter
var mockDataPath = Path.Combine(AppContext.BaseDirectory, "mock-elasticsearch-data.json");

Console.WriteLine($"[1/4] Reading data source: {mockDataPath}");

if (!File.Exists(mockDataPath))
{
    Console.WriteLine($"ERROR: Mock data file not found: {mockDataPath}");
    Console.WriteLine("Creating sample mock data file...");
    CreateSampleMockData(mockDataPath);
}

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
};

var rawData = File.ReadAllText(mockDataPath);
var logEntries = JsonSerializer.Deserialize<List<CalculationLogEntry>>(rawData, jsonOptions)!;

Console.WriteLine($"       Found {logEntries.Count} calculation log entries");
Console.WriteLine();

// =====================================================================
// STEP 2: Apply sampling/curation logic
// Goal: Select diverse test cases that cover different scenarios
// =====================================================================
Console.WriteLine("[2/4] Applying curation logic...");

var curator = new TestCaseCurator();
var curatedEntries = curator.CurateTestCases(logEntries);

Console.WriteLine($"       Selected {curatedEntries.Count} diverse test cases");
Console.WriteLine();

// =====================================================================
// STEP 3: Anonymize sensitive data
// In real production, this would replace actual CUSIPs with anonymized IDs
// =====================================================================
Console.WriteLine("[3/4] Anonymizing sensitive data...");

var anonymizer = new DataAnonymizer();
var testCases = new List<BondTestCase>();

var counter = 1;
foreach (var entry in curatedEntries)
{
    var anonymizedCase = anonymizer.AnonymizeToTestCase(entry, $"PROD-{counter:D3}");
    testCases.Add(anonymizedCase);
    counter++;
}

Console.WriteLine($"       Anonymized {testCases.Count} test cases");
Console.WriteLine();

// =====================================================================
// STEP 4: Write to golden dataset file
// =====================================================================
Console.WriteLine("[4/4] Writing golden dataset...");

// Ensure output directory exists
var outputDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}

var outputJson = JsonSerializer.Serialize(testCases, jsonOptions);
File.WriteAllText(outputPath, outputJson);

Console.WriteLine($"       Output: {outputPath}");
Console.WriteLine();

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  Export Complete!                                          ║");
Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
Console.WriteLine($"║  Test cases exported: {testCases.Count,-35} ║");
Console.WriteLine("║                                                            ║");
Console.WriteLine("║  Next steps:                                               ║");
Console.WriteLine("║  1. Review the exported test cases                         ║");
Console.WriteLine("║  2. Run tests: dotnet test                                 ║");
Console.WriteLine("║  3. Accept snapshots: dotnet verify accept                 ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

// Helper function to create sample mock data
static void CreateSampleMockData(string path)
{
    var mockData = new List<CalculationLogEntry>
    {
        new()
        {
            CorrelationId = "abc123def456",
            EventId = 9001,
            Operation = "CalculateYield",
            Input = new CalculationInput
            {
                BondParameters = new BondParameters
                {
                    Cusip = "912828XY2",  // Real CUSIP format (will be anonymized)
                    CouponRate = 4.125m,
                    MaturityDate = new DateOnly(2034, 6, 15),
                    FaceValue = 100,
                    Frequency = 2,
                    DayCountConvention = "ACT/ACT"
                },
                Price = 98.25m,
                SettlementDate = new DateOnly(2024, 6, 17)
            },
            Output = null, // Output not needed for test case generation
            Success = true,
            Timestamp = DateTime.Parse("2024-06-17T14:30:00Z")
        },
        new()
        {
            CorrelationId = "def456ghi789",
            EventId = 9001,
            Operation = "CalculateYield",
            Input = new CalculationInput
            {
                BondParameters = new BondParameters
                {
                    Cusip = "037833AK6",
                    CouponRate = 5.75m,
                    MaturityDate = new DateOnly(2029, 3, 15),
                    FaceValue = 100,
                    Frequency = 2,
                    DayCountConvention = "30/360"
                },
                Price = 103.125m,
                SettlementDate = new DateOnly(2024, 6, 17)
            },
            Output = null,
            Success = true,
            Timestamp = DateTime.Parse("2024-06-17T14:35:00Z")
        },
        new()
        {
            CorrelationId = "ghi789jkl012",
            EventId = 9001,
            Operation = "CalculateYield",
            Input = new CalculationInput
            {
                BondParameters = new BondParameters
                {
                    Cusip = "594918BP8",
                    CouponRate = 8.5m,
                    MaturityDate = new DateOnly(2028, 9, 1),
                    FaceValue = 100,
                    Frequency = 2,
                    DayCountConvention = "30/360"
                },
                Price = 96.50m,
                SettlementDate = new DateOnly(2024, 6, 17)
            },
            Output = null,
            Success = true,
            Timestamp = DateTime.Parse("2024-06-17T14:40:00Z")
        }
    };

    var json = JsonSerializer.Serialize(mockData, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
}
