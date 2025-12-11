#!/bin/bash
# Setup script for Azure Storage backend for Terraform state
#
# This script creates the Azure Storage infrastructure needed to store
# Terraform state remotely, allowing multi-machine access.
#
# This script is idempotent - it can be run multiple times safely.

set -e

# Error handling function
error_exit() {
    message="$1"
    echo "❌ Error: $message" >&2
    exit 1
}

# Configuration - Modify these values as needed
RESOURCE_GROUP_NAME="terraform-state-rg"
STORAGE_ACCOUNT_NAME="photosyncterraformstate"  # Must be globally unique (3-24 chars, lowercase alphanumeric)
CONTAINER_NAME="tfstate"
LOCATION="westeurope"  # Change to your preferred region

echo "========================================="
echo "Terraform Remote State Setup for Azure"
echo "========================================="
echo ""
echo "This script will create (if not exists):"
echo "  - Resource Group: $RESOURCE_GROUP_NAME"
echo "  - Storage Account: $STORAGE_ACCOUNT_NAME"
echo "  - Blob Container: $CONTAINER_NAME"
echo "  - Location: $LOCATION"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 1
fi

# Check if logged in to Azure
echo ""
echo "Checking Azure CLI login status..."
if ! az account show &> /dev/null; then
    echo "Not logged in to Azure. Please run: az login"
    exit 1
fi

SUBSCRIPTION_ID=$(az account show --query id -o tsv 2>/dev/null) || error_exit "Failed to get Azure subscription ID"
echo "Using subscription: $SUBSCRIPTION_ID"

# Create resource group (idempotent)
echo ""
echo "Ensuring resource group exists: $RESOURCE_GROUP_NAME..."
if az group show --name "$RESOURCE_GROUP_NAME" &> /dev/null; then
    echo "✓ Resource group already exists"
else
    echo "Creating resource group..."
    if ! az group create \
        --name "$RESOURCE_GROUP_NAME" \
        --location "$LOCATION" \
        --output table; then
        error_exit "Failed to create resource group '$RESOURCE_GROUP_NAME'"
    fi
    echo "✓ Resource group created"
fi

# Create storage account (idempotent)
echo ""
echo "Ensuring storage account exists: $STORAGE_ACCOUNT_NAME..."
if az storage account show \
    --name "$STORAGE_ACCOUNT_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" &> /dev/null; then
    echo "✓ Storage account already exists"
else
    echo "Creating storage account..."
    if ! az storage account create \
        --name "$STORAGE_ACCOUNT_NAME" \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --location "$LOCATION" \
        --sku Standard_LRS \
        --encryption-services blob \
        --https-only true \
        --min-tls-version TLS1_2 \
        --allow-blob-public-access false \
        --output table; then
        error_exit "Failed to create storage account '$STORAGE_ACCOUNT_NAME'. The name must be globally unique."
    fi
    echo "✓ Storage account created"
fi

# Get storage account key
echo ""
echo "Retrieving storage account key..."
STORAGE_KEY=$(az storage account keys list \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --query '[0].value' -o tsv 2>/dev/null) || error_exit "Failed to retrieve storage account key"

if [[ -z "$STORAGE_KEY" ]]; then
    error_exit "Storage account key is empty"
fi

# Create blob container (idempotent)
echo ""
echo "Ensuring blob container exists: $CONTAINER_NAME..."
CONTAINER_EXISTS=$(az storage container exists \
    --name "$CONTAINER_NAME" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --account-key "$STORAGE_KEY" \
    --query "exists" -o tsv 2>/dev/null) || error_exit "Failed to check if container exists"

if [[ "$CONTAINER_EXISTS" == "true" ]]; then
    echo "✓ Blob container already exists"
else
    echo "Creating blob container..."
    if ! az storage container create \
        --name "$CONTAINER_NAME" \
        --account-name "$STORAGE_ACCOUNT_NAME" \
        --account-key "$STORAGE_KEY" \
        --output table; then
        error_exit "Failed to create blob container '$CONTAINER_NAME'"
    fi
    echo "✓ Blob container created"
fi

# Enable versioning (idempotent)
echo ""
echo "Ensuring blob versioning is enabled..."
VERSIONING_ENABLED=$(az storage account blob-service-properties show \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --account-name "$STORAGE_ACCOUNT_NAME" \
    --query "isVersioningEnabled" -o tsv 2>/dev/null) || error_exit "Failed to check blob versioning status"

if [[ "$VERSIONING_ENABLED" == "true" ]]; then
    echo "✓ Blob versioning already enabled"
else
    echo "Enabling blob versioning..."
    if ! az storage account blob-service-properties update \
        --resource-group "$RESOURCE_GROUP_NAME" \
        --account-name "$STORAGE_ACCOUNT_NAME" \
        --enable-versioning true \
        --output table; then
        error_exit "Failed to enable blob versioning"
    fi
    echo "✓ Blob versioning enabled"
fi

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Next steps:"
echo "1. Edit terraform/backend.tf and uncomment the backend block"
echo "2. Update the storage_account_name if you changed it from the default"
echo "3. Run: cd terraform && terraform init -migrate-state"
echo "4. Answer 'yes' when prompted to migrate existing state to Azure"
echo ""
echo "Backend Configuration:"
echo "  resource_group_name  = \"$RESOURCE_GROUP_NAME\""
echo "  storage_account_name = \"$STORAGE_ACCOUNT_NAME\""
echo "  container_name       = \"$CONTAINER_NAME\""
echo "  key                  = \"photosync.terraform.tfstate\""
echo ""
