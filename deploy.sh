#!/bin/bash

echo "=== PhotoSync Deployment Script ==="
echo ""

# Get Function App names from Terraform
cd terraform
SOURCE1=$(terraform output -raw function_app_source1_name)
SOURCE2=$(terraform output -raw function_app_source2_name)
LOGS_URL=$(terraform output -raw logs_portal_url 2>/dev/null)
cd ..

echo "Function Apps:"
echo "  - Source 1: $SOURCE1"
echo "  - Source 2: $SOURCE2"
echo ""

# Build and publish the project
echo "=== Building and publishing project ==="
cd src
dotnet clean
dotnet publish --configuration Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build/publish failed!"
    echo "Troubleshooting tips:"
    echo "  - Check that .NET 8.0 SDK is installed (run: dotnet --version)"
    echo "  - Review the build output above for specific errors"
    echo "  - Ensure you are running this script from the project root directory"
    exit 1
fi
echo ""

# Deploy to Source 1
echo "=== Deploying to $SOURCE1 ==="
func azure functionapp publish $SOURCE1
if [ $? -ne 0 ]; then
    echo "ERROR: Deployment to $SOURCE1 failed!"
    exit 1
fi
echo ""

# Wait a moment
sleep 3

# Deploy to Source 2
echo "=== Deploying to $SOURCE2 ==="
func azure functionapp publish $SOURCE2
if [ $? -ne 0 ]; then
    echo "ERROR: Deployment to $SOURCE2 failed!"
    exit 1
fi
echo ""

# Verify deployment
echo "=== Verifying deployment ==="
echo ""
echo "Functions in $SOURCE1:"
az functionapp function list --name $SOURCE1 --resource-group PhotoSyncRG --query "[].{Name:name, Type:config.bindings[0].type}" -o table
echo ""
echo "Functions in $SOURCE2:"
az functionapp function list --name $SOURCE2 --resource-group PhotoSyncRG --query "[].{Name:name, Type:config.bindings[0].type}" -o table
echo ""

echo "=== Deployment complete! ==="
echo ""
echo "To view logs in Application Insights:"
if [ ! -z "$LOGS_URL" ]; then
    echo "  $LOGS_URL"
else
    echo "  Azure Portal → Application Insights → photosync-insights → Logs"
fi
echo ""
echo "To trigger manually:"
echo "  ./trigger-sync.sh                 # Trigger both Function Apps"
echo "  ./trigger-sync.sh --source1-only  # Trigger Source 1 only"
echo "  ./trigger-sync.sh --logs          # Trigger and open logs"
