# Bond Calculation Regression Testing POC

A proof-of-concept demonstrating regression testing for a bond calculation service that wraps a third-party quant library. This approach captures production calculation data and uses snapshot testing to detect changes after library upgrades.

## Overview

When you depend on a third-party calculation library (like QuantLib), upgrading versions can introduce subtle changes in calculation results. This POC demonstrates a workflow to:

1. **Capture** production calculation inputs/outputs via structured logging
2. **Curate** diverse test cases from production data
3. **Snapshot test** against committed baselines using Verify
4. **Review and accept** changes after library upgrades

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PRODUCTION ENVIRONMENT                           │
│                                                                         │
│  ┌─────────────────────┐      ┌────────────────────┐                   │
│  │  BondCalculation    │      │    Elasticsearch   │                   │
│  │  Service            │─────▶│    (EventId 9001)  │                   │
│  │  (Serilog logging)  │      │                    │                   │
│  └─────────────────────┘      └─────────┬──────────┘                   │
└─────────────────────────────────────────┼───────────────────────────────┘
                                          │
                                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        DATA EXPORT WORKFLOW                             │
│                                                                         │
│  ┌─────────────────────┐      ┌────────────────────┐                   │
│  │   DataExporter      │      │   Golden Dataset   │                   │
│  │   Console Tool      │─────▶│   (JSON files)     │                   │
│  │   - Query ES        │      │   - Anonymized     │                   │
│  │   - Curate cases    │      │   - Versioned      │                   │
│  │   - Anonymize       │      └────────────────────┘                   │
│  └─────────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────────┘
                                          │
                                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        REGRESSION TESTING                               │
│                                                                         │
│  ┌─────────────────────┐      ┌────────────────────┐                   │
│  │   xUnit Tests       │      │   Verify Snapshots │                   │
│  │   with Verify       │◀────▶│   (.verified.txt)  │                   │
│  │                     │      │                    │                   │
│  └─────────────────────┘      └────────────────────┘                   │
│                                                                         │
│  On library upgrade: Tests fail if results change → Review → Accept    │
└─────────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
bond-regression-testing/
├── BondRegressionTesting.sln
├── README.md
│
├── src/
│   └── BondCalculationService/
│       ├── Models/
│       │   ├── BondParameters.cs       # Input parameters for calculations
│       │   ├── YieldResult.cs          # Calculation output
│       │   ├── BondTestCase.cs         # Golden dataset test case format
│       │   └── CalculationLogEntry.cs  # Elasticsearch log schema
│       ├── Services/
│       │   ├── IBondCalculationService.cs
│       │   └── BondCalculationService.cs  # Main service with logging
│       ├── Configuration/
│       │   └── TestDataCaptureOptions.cs
│       ├── appsettings.json            # Serilog + ES configuration
│       └── Program.cs
│
├── tests/
│   └── BondCalculationService.Tests/
│       ├── BondYieldRegressionTests.cs  # Snapshot tests
│       ├── ModuleInitializer.cs         # Verify configuration
│       ├── GoldenDataset/
│       │   ├── synthetic-cases.json     # Hand-crafted test cases
│       │   └── production-cases.json    # Cases from production
│       └── Snapshots/                   # Verify snapshot files
│
└── tools/
    └── DataExporter/
        ├── Program.cs                   # Main export tool
        ├── TestCaseCurator.cs           # Selects diverse cases
        ├── DataAnonymizer.cs            # Anonymizes sensitive data
        ├── mock-elasticsearch-data.json # POC mock data
        └── appsettings.json
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- (Optional) Elasticsearch for production data capture

### Build the Solution

```bash
cd bond-regression-testing
dotnet build
```

### Run Tests (First Time - Generates Snapshots)

```bash
dotnet test
```

On first run, Verify will create `.verified.txt` snapshot files in the `Snapshots/` directory. These should be committed to source control.

### View Generated Snapshots

After running tests, check the generated snapshots:

```bash
ls tests/BondCalculationService.Tests/Snapshots/
```

Each test case produces a snapshot file like:
```
BondYieldRegressionTests.CalculateYield_SyntheticCases_MatchesSnapshot_SYN-001.verified.txt
```

## Regression Testing Workflow

### Normal Development

Tests compare calculation results against committed snapshots:

```bash
dotnet test
```

If all tests pass, calculations match the baseline.

### After Library Upgrade

1. **Upgrade the library** (in production, update the QuantLib reference)

2. **Run tests** - they may fail if calculations changed:
   ```bash
   dotnet test
   ```

3. **Review changes** - Verify creates `.received.txt` files for comparison:
   ```bash
   # Use your preferred diff tool
   diff tests/BondCalculationService.Tests/Snapshots/*.received.txt \
        tests/BondCalculationService.Tests/Snapshots/*.verified.txt
   ```

4. **Accept new snapshots** if changes are expected:
   ```bash
   # Accept all changed snapshots
   dotnet verify accept

   # Or accept specific files
   dotnet verify accept --target tests/BondCalculationService.Tests/Snapshots/
   ```

5. **Commit the updated snapshots**:
   ```bash
   git add tests/BondCalculationService.Tests/Snapshots/
   git commit -m "Update snapshots for QuantLib v2.0 upgrade"
   ```

### Export Production Data to Golden Dataset

1. **Run the DataExporter tool**:
   ```bash
   dotnet run --project tools/DataExporter
   ```

2. **Review exported cases** in `tests/GoldenDataset/production-cases.json`

3. **Run tests to generate new snapshots**:
   ```bash
   dotnet test
   ```

4. **Accept and commit**:
   ```bash
   dotnet verify accept
   git add tests/
   git commit -m "Add new production test cases"
   ```

## Configuration

### Enable/Disable Test Data Capture

In `appsettings.json`:

```json
{
  "TestDataCapture": {
    "Enabled": true,
    "EventId": 9001,
    "SamplingRate": 1.0
  }
}
```

### Serilog Elasticsearch Sink

The Elasticsearch sink is configured to only capture EventId 9001:

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "bond-calculations-{0:yyyy.MM}"
        }
      }
    ],
    "Filter": [
      {
        "Name": "ByIncludingOnly",
        "Args": {
          "expression": "EventId.Id = 9001"
        }
      }
    ]
  }
}
```

## Test Case Types

### Synthetic Cases (`synthetic-cases.json`)

Hand-crafted test cases covering known scenarios:
- Par bonds (price = face value)
- Premium bonds (price > face value)
- Discount bonds (price < face value)
- Short/long duration
- Different coupon frequencies

### Production Cases (`production-cases.json`)

Real (anonymized) cases captured from production:
- Represent actual usage patterns
- May reveal edge cases not covered by synthetic cases
- Automatically curated for diversity

## Key Design Decisions

### Why EventId 9001?

Using a specific EventId allows:
- Filtering at the Serilog level (only relevant logs go to ES)
- Easy querying in Elasticsearch
- Separation of test data from operational logs

### Why Anonymize?

Production data may contain:
- Proprietary CUSIP/ISIN identifiers
- Trading patterns revealed by settlement dates
- Client-specific information

### Why Verify Instead of Approval Tests?

Verify provides:
- Clean JSON snapshot format
- Built-in diff tooling integration
- Easy acceptance workflow via CLI
- Strong xUnit integration

## Extending This POC

### Adding Real Elasticsearch Connection

Replace mock data reading with actual ES query:

```csharp
// In DataExporter/Program.cs
var client = new ElasticClient(new ConnectionSettings(new Uri(esUrl)));
var response = await client.SearchAsync<CalculationLogEntry>(s => s
    .Index("bond-calculations-*")
    .Query(q => q.Term(t => t.EventId, 9001))
    .Size(1000));
```

### Adding More Analytics to YieldResult

Add new fields to `YieldResult.cs` and update the snapshot comparison in `BondYieldRegressionTests.cs`.

### Custom Curation Logic

Modify `TestCaseCurator.cs` to select cases based on your specific needs.

## Troubleshooting

### Tests Failing After Clean Clone

Snapshots need to be generated on first run:
```bash
dotnet test
dotnet verify accept
```

### Too Many Snapshot Diffs

Consider:
- Rounding results to fewer decimal places
- Excluding volatile fields from snapshots
- Grouping related cases

### Missing Dependencies

```bash
dotnet restore
```

## Technologies Used

- **ASP.NET Core 8.0** - Web service framework
- **Serilog** - Structured logging
- **Serilog.Sinks.Elasticsearch** - ES integration
- **xUnit** - Test framework
- **FluentAssertions** - Readable assertions
- **Verify.Xunit** - Snapshot testing

## License

This is a proof-of-concept for demonstration purposes.
