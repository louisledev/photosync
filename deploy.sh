#!/bin/bash

echo "=== PhotoSync Deployment Script ==="
echo ""

# Get Function App names from Terraform
cd terraform
SOURCE1=$(terraform output -raw function_app_source1_name)
SOURCE2=$(terraform output -raw function_app_source2_name)
cd ..

echo "Function Apps:"
echo "  - Source 1: $SOURCE1"
echo "  - Source 2: $SOURCE2"
echo ""

# Build the project
echo "=== Building project ==="
cd src
dotnet clean
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed!"
    exit 1
fi
echo ""

# Deploy to Source 1
echo "=== Deploying to $SOURCE1 ==="
func azure functionapp publish $SOURCE1 --dotnet-isolated || {
    echo "WARNING: Deployment returned an error, but continuing..."
    echo "This is often a false positive 'sync triggers' error."
}
echo ""

# Wait a moment
sleep 5

# Deploy to Source 2
echo "=== Deploying to $SOURCE2 ==="
func azure functionapp publish $SOURCE2 --dotnet-isolated || {
    echo "WARNING: Deployment returned an error, but continuing..."
    echo "This is often a false positive 'sync triggers' error."
}
echo ""

# Wait for deployment to stabilize
sleep 5

# Restart both Function Apps to ensure triggers are registered
echo "=== Restarting Function Apps ==="
az functionapp restart --name $SOURCE1 --resource-group PhotoSyncRG
az functionapp restart --name $SOURCE2 --resource-group PhotoSyncRG
echo ""

# Wait for restart
echo "Waiting for Function Apps to start..."
sleep 30

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
echo "To view logs:"
echo "  az functionapp log tail --name $SOURCE1 --resource-group PhotoSyncRG"
echo ""
echo "To trigger manually:"
echo "  az functionapp function invoke --name $SOURCE1 --resource-group PhotoSyncRG --function-name PhotoSyncTimer"
