#!/bin/bash

echo "=== PhotoSync Manual Trigger Script ==="
echo ""

# Function to open URLs in a cross-platform way
open_url() {
    local url="$1"
    
    # Detect the platform and use appropriate command
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        open "$url"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        if command -v xdg-open &> /dev/null; then
            xdg-open "$url"
        else
            echo "Warning: xdg-open not found. Please install xdg-utils or open the URL manually:"
            echo "$url"
        fi
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "win32" ]]; then
        # Windows (Git Bash, Cygwin, or native)
        start "$url"
    else
        # WSL or unknown platform
        if grep -qi microsoft /proc/version 2>/dev/null; then
            # WSL
            cmd.exe /c start "$url"
        else
            echo "Warning: Unknown platform. Please open the URL manually:"
            echo "$url"
        fi
    fi
}

# Get Function App names from Terraform
cd terraform
SOURCE1=$(terraform output -raw function_app_source1_name 2>/dev/null)
SOURCE2=$(terraform output -raw function_app_source2_name 2>/dev/null)
cd ..

if [ -z "$SOURCE1" ] || [ -z "$SOURCE2" ]; then
    echo "ERROR: Could not get Function App names from Terraform"
    echo "Make sure you're running this from the project root and Terraform has been applied"
    exit 1
fi

echo "Function Apps:"
echo "  - Source 1: $SOURCE1"
echo "  - Source 2: $SOURCE2"
echo ""

# Parse command line arguments
TRIGGER_SOURCE1=true
TRIGGER_SOURCE2=true
SHOW_LOGS=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --source1-only)
            TRIGGER_SOURCE2=false
            shift
            ;;
        --source2-only)
            TRIGGER_SOURCE1=false
            shift
            ;;
        --logs)
            SHOW_LOGS=true
            shift
            ;;
        --help)
            echo "Usage: ./trigger-sync.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --source1-only    Only trigger Source 1 Function App"
            echo "  --source2-only    Only trigger Source 2 Function App"
            echo "  --logs            View logs in Azure Portal after triggering"
            echo "  --help            Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./trigger-sync.sh                 # Trigger both Function Apps"
            echo "  ./trigger-sync.sh --source1-only  # Only trigger Source 1"
            echo "  ./trigger-sync.sh --logs          # Trigger and view logs"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Trigger Source 1
if [ "$TRIGGER_SOURCE1" = true ]; then
    echo "=== Triggering $SOURCE1 ==="
    FUNCTION_KEY=$(az functionapp function keys list \
        --name $SOURCE1 \
        --resource-group PhotoSyncRG \
        --function-name ManualSync \
        --query "default" \
        -o tsv 2>/dev/null)

    if [ -z "$FUNCTION_KEY" ]; then
        echo "ERROR: Could not get function key for $SOURCE1"
        exit 1
    fi

    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
        "https://$SOURCE1.azurewebsites.net/api/manualsync?code=$FUNCTION_KEY")

    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')

    if [ "$HTTP_CODE" = "200" ]; then
        echo "✓ Successfully triggered $SOURCE1"
        if [ ! -z "$BODY" ]; then
            echo "  Response: $BODY"
        fi
    else
        echo "✗ Failed to trigger $SOURCE1 (HTTP $HTTP_CODE)"
        if [ ! -z "$BODY" ]; then
            echo "  Error: $BODY"
        fi
    fi
    echo ""
fi

# Trigger Source 2
if [ "$TRIGGER_SOURCE2" = true ]; then
    echo "=== Triggering $SOURCE2 ==="
    FUNCTION_KEY=$(az functionapp function keys list \
        --name $SOURCE2 \
        --resource-group PhotoSyncRG \
        --function-name ManualSync \
        --query "default" \
        -o tsv 2>/dev/null)

    if [ -z "$FUNCTION_KEY" ]; then
        echo "ERROR: Could not get function key for $SOURCE2"
        exit 1
    fi

    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
        "https://$SOURCE2.azurewebsites.net/api/manualsync?code=$FUNCTION_KEY")

    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')

    if [ "$HTTP_CODE" = "200" ]; then
        echo "✓ Successfully triggered $SOURCE2"
        if [ ! -z "$BODY" ]; then
            echo "  Response: $BODY"
        fi
    else
        echo "✗ Failed to trigger $SOURCE2 (HTTP $HTTP_CODE)"
        if [ ! -z "$BODY" ]; then
            echo "  Error: $BODY"
        fi
    fi
    echo ""
fi

# Show logs if requested
if [ "$SHOW_LOGS" = true ]; then
    echo "=== Opening Application Insights logs ==="

    # Get Application Insights URL from Terraform
    cd terraform
    LOGS_URL=$(terraform output -raw logs_portal_url 2>/dev/null)
    cd ..

    if [ ! -z "$LOGS_URL" ]; then
        echo "Opening Application Insights..."
        open_url "$LOGS_URL"
    else
        echo "Application Insights not found. Opening Function App logs instead..."
        if [ "$TRIGGER_SOURCE1" = true ] && [ "$TRIGGER_SOURCE2" = false ]; then
            open_url "https://portal.azure.com/#@/resource/subscriptions/$(az account show --query id -o tsv)/resourceGroups/PhotoSyncRG/providers/Microsoft.Web/sites/$SOURCE1/appServices"
        elif [ "$TRIGGER_SOURCE2" = true ] && [ "$TRIGGER_SOURCE1" = false ]; then
            open_url "https://portal.azure.com/#@/resource/subscriptions/$(az account show --query id -o tsv)/resourceGroups/PhotoSyncRG/providers/Microsoft.Web/sites/$SOURCE2/appServices"
        else
            open_url "https://portal.azure.com/#@/resource/subscriptions/$(az account show --query id -o tsv)/resourceGroups/PhotoSyncRG/providers/Microsoft.Web/sites/$SOURCE1/appServices"
        fi
    fi
fi

echo "=== Done! ==="
echo ""
echo "To view logs:"
echo "  - Application Insights: Run './trigger-sync.sh --logs' or use Terraform output"
echo "  - Azure Portal: https://portal.azure.com → Application Insights → photosync-insights"
echo "  - Or check your destination OneDrive for synced photos"
