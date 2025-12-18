# Code Coverage Visualization Guide

## Test Structure

This project has comprehensive test coverage with two test suites:

- **Unit Tests** (74 tests): `tests/PhotoSync.Tests/`
  - Fast, isolated tests using mocks
  - Tests business logic, state management, configuration validation
  - No external dependencies required

- **Integration Tests** (9 tests): `tests/PhotoSync.IntegrationTests/`
  - End-to-end tests with real Azure Storage (via Azurite/Testcontainers)
  - Tests actual table storage operations and full sync workflows
  - Requires Docker for Testcontainers

## Quick Start

### One-Command Solution
```bash
./generate-coverage-report.sh
```
This script will:
1. Run unit tests (74 tests) with coverage collection
2. Run integration tests (9 tests) with coverage collection
3. Generate a combined HTML report from both test suites
4. Track coverage history over time
5. Open the report in your browser automatically

## What the HTML Report Shows

### Main Dashboard
![Dashboard Example](https://via.placeholder.com/800x400/4CAF50/FFFFFF?text=Coverage+Dashboard)

The main page displays:
- **Line Coverage**: Percentage of code lines executed by tests
- **Branch Coverage**: Percentage of conditional branches tested
- **Method Coverage**: Percentage of methods called by tests
- **Summary Table**: Coverage by namespace/class

### Coverage Visualization

**Green Lines** ✅: Code that was executed during tests
```csharp
public void MyMethod() {
    Console.WriteLine("This line is covered");  // ✅ Green
    return;                                      // ✅ Green
}
```

**Red Lines** ❌: Code that was NOT executed
```csharp
public void UntestedMethod() {
    Console.WriteLine("Never called in tests");  // ❌ Red
    throw new Exception("Not covered");          // ❌ Red
}
```

**Yellow Lines** ⚠️: Partially covered (some branches not tested)
```csharp
if (condition)  // ⚠️ Yellow - only true branch tested
    DoSomething();  // ✅ Green
else
    DoSomethingElse();  // ❌ Red
```

### Navigation Features

1. **File Explorer**: Click on any file to see detailed coverage
2. **Search**: Find specific classes or methods
3. **Filters**: Show only covered/uncovered code
4. **History**: Track coverage trends over time (with `-historydir` option)

## Manual Report Generation

### Step-by-Step

1. **Install ReportGenerator** (one-time):
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   ```

2. **Run unit tests with coverage**:
   ```bash
   cd tests/PhotoSync.Tests
   dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"
   cd ../..
   ```

3. **Run integration tests with coverage**:
   ```bash
   cd tests/PhotoSync.IntegrationTests
   dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"
   cd ../..
   ```

4. **Generate combined HTML report**:
   ```bash
   reportgenerator \
     -reports:"tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/*/coverage.cobertura.xml" \
     -targetdir:"TestResults/coverage-report" \
     -reporttypes:"Html;Badges;HtmlSummary;JsonSummary" \
     -historydir:"TestResults/coverage-history"
   ```

5. **Open the report**:
   ```bash
   open TestResults/coverage-report/index.html
   ```

## Report Types

ReportGenerator supports multiple output formats:

### HTML Reports
```bash
-reporttypes:"Html"              # Standard HTML
-reporttypes:"HtmlInline"        # Single HTML file
-reporttypes:"HtmlInline_AzurePipelines"  # Azure DevOps optimized
-reporttypes:"HtmlSummary"       # Summary only
```

### Badges
```bash
-reporttypes:"Badges"            # SVG badges for README
```
Example badges generated:
- `badge_linecoverage.svg` - ![Coverage](https://img.shields.io/badge/coverage-52%25-yellow)
- `badge_branchcoverage.svg`
- `badge_methodcoverage.svg`

### Other Formats
```bash
-reporttypes:"JsonSummary"       # JSON format
-reporttypes:"Xml"               # XML format
-reporttypes:"Cobertura"         # Cobertura XML
-reporttypes:"TeamCitySummary"   # TeamCity integration
```

### Multiple Reports
```bash
-reporttypes:"Html;Badges;JsonSummary"  # Generate multiple formats
```

## Advanced Options

### Track Coverage History
```bash
reportgenerator \
  -reports:"tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/coverage-report" \
  -reporttypes:"Html" \
  -historydir:"TestResults/coverage-history"
```
This tracks coverage changes over time. The generate-coverage-report.sh script automatically uses this option.

### Filter Specific Files
```bash
reportgenerator \
  -reports:"tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/coverage-report" \
  -reporttypes:"Html" \
  -classfilters:"+PhotoSync.*;-*.Tests.*;-*.IntegrationTests.*"
```
- `+` = include
- `-` = exclude

### Set Coverage Thresholds
```bash
reportgenerator \
  -reports:"tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/coverage-report" \
  -reporttypes:"Html" \
  -verbosity:"Info" \
  -tag:"$(date +%Y%m%d_%H%M%S)"
```

## CI/CD Integration

### GitHub Actions
```yaml
- name: Run Unit Tests
  run: |
    cd tests/PhotoSync.Tests
    dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"

- name: Run Integration Tests
  run: |
    cd tests/PhotoSync.IntegrationTests
    dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults"

- name: Generate Coverage Report
  run: |
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator \
      -reports:"tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/*/coverage.cobertura.xml" \
      -targetdir:"TestResults/coverage-report" \
      -reporttypes:"Html;Badges;HtmlSummary;JsonSummary" \
      -historydir:"TestResults/coverage-history"

- name: Upload Coverage
  uses: actions/upload-artifact@v3
  with:
    name: coverage-report
    path: TestResults/coverage-report/
```

### Azure Pipelines
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: 'tests/PhotoSync.Tests/*.csproj'
    arguments: '--collect:"XPlat Code Coverage" --results-directory:"./TestResults"'

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    projects: 'tests/PhotoSync.IntegrationTests/*.csproj'
    arguments: '--collect:"XPlat Code Coverage" --results-directory:"./TestResults"'

- task: reportgenerator@5
  displayName: 'Generate Coverage Report'
  inputs:
    reports: 'tests/PhotoSync.Tests/TestResults/**/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/**/coverage.cobertura.xml'
    targetdir: '$(Build.ArtifactStagingDirectory)/coverage-report'
    reporttypes: 'HtmlInline_AzurePipelines;Badges;HtmlSummary;JsonSummary'
    historydir: '$(Build.ArtifactStagingDirectory)/coverage-history'
```

## Understanding Coverage Metrics

### Line Coverage
```
Line Coverage = (Covered Lines / Total Lines) × 100%
```
**Good Target**: 70-80%

### Branch Coverage
```
Branch Coverage = (Covered Branches / Total Branches) × 100%
```
**Good Target**: 60-70%

### Method Coverage
```
Method Coverage = (Covered Methods / Total Methods) × 100%
```
**Good Target**: 80-90%

## Tips for Better Coverage

1. **Focus on Critical Code**: Prioritize business logic over boilerplate
2. **Test Edge Cases**: Cover error paths and boundary conditions
3. **Avoid Testing Private Methods**: Test public APIs instead
4. **Use Coverage as a Guide**: High coverage ≠ good tests
5. **Review Uncovered Code**: Understand why code isn't covered

## Current Project Status

Based on your latest test run:
- ✅ Unit Tests: 74/74 passing (100%)
- ✅ Integration Tests: 9/9 passing (100%)
- ✅ Total: 83/83 tests passing (100%)
- 📊 Coverage report combines results from both test suites
- 📈 Coverage history tracks trends over time

## File Locations

### Test Results
- **Unit Test Coverage XML**: `tests/PhotoSync.Tests/TestResults/{guid}/coverage.cobertura.xml`
- **Integration Test Coverage XML**: `tests/PhotoSync.IntegrationTests/TestResults/{guid}/coverage.cobertura.xml`

### Combined Coverage Report
- **HTML Report**: `TestResults/coverage-report/index.html`
- **Summary**: `TestResults/coverage-report/Summary.txt`
- **Badges**: `TestResults/coverage-report/badge_*.svg`
- **JSON Summary**: `TestResults/coverage-report/Summary.json`
- **Coverage History**: `TestResults/coverage-history/`

### Scripts
- **Generation Script**: `./generate-coverage-report.sh`

## Troubleshooting

### "No coverage data found"
- Make sure tests ran successfully: `dotnet test`
- Check TestResults directory exists
- Verify coverage.cobertura.xml file is present

### "Report is empty"
- Run tests before generating report
- Check file paths in reportgenerator command
- Use absolute paths if relative paths fail

### "ReportGenerator not found"
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### "Browser doesn't open"
```bash
# Manually open the combined report
open TestResults/coverage-report/index.html

# Or use a specific browser
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome TestResults/coverage-report/index.html
```

### "Integration tests fail"
- Make sure Docker is running (required for Testcontainers)
- Azurite container will be automatically started and stopped by Testcontainers
- Check Docker logs if tests hang or fail to connect

## Additional Resources

- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [Code Coverage Best Practices](https://martinfowler.com/bliki/TestCoverage.html)
