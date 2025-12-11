#!/bin/bash

# Generate Code Coverage Report
# This script runs both unit and integration tests with coverage and generates a combined HTML report

set -e

# Ensure we're in the project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "🧪 Running Unit Tests with code coverage..."
cd tests/PhotoSync.Tests
dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults" --verbosity:minimal
UNIT_TEST_STATUS=$?
cd ../..

echo ""
echo "🔬 Running Integration Tests with code coverage..."
cd tests/PhotoSync.IntegrationTests
dotnet test --collect:"XPlat Code Coverage" --results-directory:"./TestResults" --verbosity:minimal
INTEGRATION_TEST_STATUS=$?
cd ../..

# Check if tests passed
if [[ $UNIT_TEST_STATUS -ne 0 ]] || [[ $INTEGRATION_TEST_STATUS -ne 0 ]]; then
    echo ""
    echo "⚠️  Some tests failed, but continuing with coverage report generation..."
fi

echo ""
echo "📊 Generating combined HTML coverage report..."

# Create combined coverage report from both test projects
reportgenerator \
  -reports:"tests/PhotoSync.Tests/TestResults/*/coverage.cobertura.xml;tests/PhotoSync.IntegrationTests/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/coverage-report" \
  -reporttypes:"Html;Badges;HtmlSummary;JsonSummary" \
  -historydir:"TestResults/coverage-history"

echo ""
echo "✅ Coverage report generated!"
echo "📂 Location: TestResults/coverage-report/index.html"
echo ""
echo "📈 Coverage Summary:"
echo "===================="
cat TestResults/coverage-report/Summary.txt 2>/dev/null || echo "Summary not available"

echo ""
echo "🧪 Test Results:"
echo "  Unit Tests: $([[ $UNIT_TEST_STATUS -eq 0 ]] && echo '✅ PASSED' || echo '❌ FAILED')"
echo "  Integration Tests: $([[ $INTEGRATION_TEST_STATUS -eq 0 ]] && echo '✅ PASSED' || echo '❌ FAILED')"

echo ""
echo "🌐 Opening report in browser..."
open TestResults/coverage-report/index.html

exit $((UNIT_TEST_STATUS + INTEGRATION_TEST_STATUS))
