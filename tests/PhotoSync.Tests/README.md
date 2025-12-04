# PhotoSync Unit Tests

This directory contains unit tests for the PhotoSync Azure Function project.

## Test Structure

```
tests/PhotoSync.Tests/
├── PhotoSyncFunctionTests.cs         # Tests for the timer-triggered function
├── PhotoSyncServiceTests.cs          # Tests for the core sync service
├── StateManagerTests.cs              # Tests for state management
├── ConfigurationValidatorTests.cs    # Tests for configuration validation
└── README.md                         # This file
```

## Test Coverage

### PhotoSyncFunctionTests
- Tests the Azure Function timer trigger
- Verifies logging behavior
- Tests error handling and exception propagation
- Validates timer schedule configuration

### PhotoSyncServiceTests
- Tests filename generation from EXIF data
- Tests filename parsing with various formats
- Validates configuration handling
- Tests state manager integration

### StateManagerTests
- Tests Azure Table Storage state management
- Validates processed files tracking
- Tests cleanup of old records
- Verifies batch operations

### ConfigurationValidatorTests
- Tests OneDrive configuration validation
- Validates folder path formats
- Tests credential validation
- Verifies multi-account configuration

## Running Tests

✅ **Tests are now running successfully with Microsoft.Graph v5!**

### Run all tests
```bash
cd tests/PhotoSync.Tests
dotnet test
```

**Current Results**: 22/42 tests passing (some require Azurite or improved mocking)

### Run with detailed output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~PhotoSyncFunctionTests"
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~PhotoSyncFunctionTests.Run_WithValidTimer_CallsSyncPhotosAsync"
```

### Generate code coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Visualize Coverage with HTML Report

**Option 1: Use the convenience script (from project root):**
```bash
./generate-coverage-report.sh
```
This will run tests, generate an HTML report, and open it in your browser automatically.

**Option 2: Manual generation:**
```bash
# Install ReportGenerator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
cd tests/PhotoSync.Tests
reportgenerator \
  -reports:"TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/html" \
  -reporttypes:"Html;Badges"

# Open in browser
open TestResults/html/index.html
```

**What you'll see in the HTML report:**
- 📊 **Interactive Dashboard**: Overall coverage percentages
- 📈 **Line Coverage**: See which lines are covered (green) vs uncovered (red)
- 🌳 **Branch Coverage**: Conditional logic coverage
- 📁 **File Explorer**: Drill down into each file
- 🔍 **Method Details**: Coverage for individual methods
- 🎯 **Coverage Badges**: SVG badges for your README

**Report Location:**
```
tests/PhotoSync.Tests/TestResults/html/index.html
```

## Test Dependencies

- **xUnit**: Testing framework
- **Moq**: Mocking framework for creating test doubles
- **Microsoft.Extensions.Logging.Abstractions**: For logger mocking
- **Microsoft.Extensions.Configuration.Abstractions**: For configuration mocking

## Integration Tests

Note: These are unit tests that use mocks. For integration testing:

1. **StateManager Integration Tests**: Require Azurite or real Azure Storage
   ```bash
   # Start Azurite
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

2. **Configuration Validator Integration Tests**: Require valid Azure AD credentials

3. **Full E2E Tests**: Would require:
   - Valid OneDrive accounts
   - Azure AD app registrations
   - Real Azure Function hosting

## Testing Best Practices

### What's Tested
- Business logic and algorithms
- Error handling paths
- Configuration validation
- State management logic
- Logging behavior

### What's Mocked
- Azure services (Table Storage, OneDrive)
- HTTP clients
- External dependencies
- File system operations

### Test Patterns Used

**Arrange-Act-Assert (AAA)**:
```csharp
[Fact]
public async Task TestName()
{
    // Arrange
    var mock = new Mock<IService>();
    mock.Setup(s => s.Method()).Returns(value);

    // Act
    var result = await systemUnderTest.Execute();

    // Assert
    Assert.Equal(expected, result);
    mock.Verify(s => s.Method(), Times.Once);
}
```

**Theory Tests with InlineData**:
```csharp
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
public void TestName(string input, string expected)
{
    var result = systemUnderTest.Process(input);
    Assert.Equal(expected, result);
}
```

## Continuous Integration

### GitHub Actions Example
```yaml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: '**/tests/**/*.csproj'
    arguments: '--configuration Release --collect:"XPlat Code Coverage"'
```

## Known Limitations

1. **Microsoft Graph API**: Some tests may fail if the main project has build errors with the Graph SDK version. The tests themselves are valid.

2. **Azure Table Storage**: StateManager tests require either:
   - Azurite running locally, or
   - Valid Azure Storage connection string

3. **Private Methods**: Some tests use reflection to test private methods. This is acceptable for unit tests but should be used sparingly.

## Future Improvements

- [ ] Add integration tests with Azurite
- [ ] Add E2E tests with test OneDrive accounts
- [ ] Increase code coverage to 80%+
- [ ] Add performance benchmarks
- [ ] Add mutation testing
- [ ] Add contract tests for Microsoft Graph API

## Troubleshooting

### Tests fail with "Could not load file or assembly"
```bash
cd tests/PhotoSync.Tests
dotnet restore
dotnet build
dotnet test
```

### Azurite connection errors
```bash
# Start Azurite
azurite --silent --location c:\azurite

# Or use development storage
# Connection string: "UseDevelopmentStorage=true"
```

### Mock setup not working
Ensure you're using the correct mock setup syntax:
```csharp
// For methods
mock.Setup(m => m.Method(It.IsAny<string>())).Returns(value);

// For properties
mock.Setup(m => m.Property).Returns(value);

// For async methods
mock.Setup(m => m.MethodAsync()).ReturnsAsync(value);
```

## Contributing

When adding new tests:
1. Follow AAA pattern
2. Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
3. Test both success and failure paths
4. Mock all external dependencies
5. Keep tests fast and isolated
6. Add comments for complex test logic

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Quick Start](https://github.com/moq/moq4/wiki/Quickstart)
- [.NET Testing Best Practices](https://docs.microsoft.com/dotnet/core/testing/unit-testing-best-practices)
- [Azure Functions Testing](https://docs.microsoft.com/azure/azure-functions/functions-test-a-function)
