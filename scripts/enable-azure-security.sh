#!/bin/bash
# Enable Azure Security Features for PhotoSync

# Error handling function
error() {
    echo "$@" >&2
    return 1
}

set -e

RESOURCE_GROUP="PhotoSyncRG"
SUBSCRIPTION_ID="${ARM_SUBSCRIPTION_ID:-$(az account show --query id -o tsv)}"

echo "=== Enabling Azure Security Features ==="
echo "Subscription: $SUBSCRIPTION_ID"
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# Enable Microsoft Defender for Cloud (Free tier)
echo "1. Enabling Microsoft Defender for Cloud..."
az security pricing create \
  --name StorageAccounts \
  --tier Free \
  --subscription "$SUBSCRIPTION_ID" || echo "Storage defender already configured"

az security pricing create \
  --name AppServices \
  --tier Free \
  --subscription "$SUBSCRIPTION_ID" || echo "App Services defender already configured"

az security pricing create \
  --name KeyVaults \
  --tier Free \
  --subscription "$SUBSCRIPTION_ID" || echo "Key Vault defender already configured"

# Enable diagnostic settings for Function Apps
echo ""
echo "2. Enabling diagnostic settings for Function Apps..."

for FUNCTION_APP in "photosync-source1" "photosync-source2"; do
  echo "   Configuring $FUNCTION_APP..."

  # Get Function App resource ID
  FUNCTION_APP_ID=$(az functionapp show \
    --name "$FUNCTION_APP" \
    --resource-group "$RESOURCE_GROUP" \
    --query id -o tsv)

  # Enable diagnostic settings (logs to Application Insights)
  az monitor diagnostic-settings create \
    --name "${FUNCTION_APP}-security-logs" \
    --resource "$FUNCTION_APP_ID" \
    --logs '[
      {
        "category": "FunctionAppLogs",
        "enabled": true,
        "retentionPolicy": {
          "enabled": true,
          "days": 30
        }
      }
    ]' \
    --metrics '[
      {
        "category": "AllMetrics",
        "enabled": true,
        "retentionPolicy": {
          "enabled": true,
          "days": 30
        }
      }
    ]' \
    --workspace "$(az monitor log-analytics workspace list --resource-group "$RESOURCE_GROUP" --query '[0].id' -o tsv)" \
    2>/dev/null || echo "   Diagnostic settings already exist for $FUNCTION_APP"
done

# Enable Key Vault diagnostic settings
echo ""
echo "3. Enabling diagnostic settings for Key Vault..."
KEY_VAULT_NAME=$(az keyvault list --resource-group "$RESOURCE_GROUP" --query '[0].name' -o tsv)

if [[ -n "$KEY_VAULT_NAME" ]]; then
  KEY_VAULT_ID=$(az keyvault show --name "$KEY_VAULT_NAME" --query id -o tsv)

  az monitor diagnostic-settings create \
    --name "keyvault-security-logs" \
    --resource "$KEY_VAULT_ID" \
    --logs '[
      {
        "category": "AuditEvent",
        "enabled": true,
        "retentionPolicy": {
          "enabled": true,
          "days": 30
        }
      }
    ]' \
    --workspace "$(az monitor log-analytics workspace list --resource-group "$RESOURCE_GROUP" --query '[0].id' -o tsv)" \
    2>/dev/null || echo "   Diagnostic settings already exist for Key Vault"
fi

# Enable alerts for security events
echo ""
echo "4. Creating security alerts..."

# Alert for Function App failures
az monitor metrics alert create \
  --name "photosync-function-failures" \
  --resource-group "$RESOURCE_GROUP" \
  --scopes $(az functionapp show --name photosync-source1 --resource-group "$RESOURCE_GROUP" --query id -o tsv) \
  --condition "count Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --description "Alert when Function App has more than 10 HTTP 5xx errors in 5 minutes" \
  2>/dev/null || echo "   Alert already exists"

echo ""
echo "=== Azure Security Configuration Complete ==="
echo ""
echo "Next steps:"
echo "1. Visit Azure Security Center: https://portal.azure.com/#blade/Microsoft_Azure_Security/SecurityMenuBlade/0"
echo "2. Review security recommendations for your resources"
echo "3. Enable Microsoft Defender for Cloud Standard tier for enhanced protection (paid)"
