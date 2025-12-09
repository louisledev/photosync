# Security Module

This Terraform module configures comprehensive security monitoring and alerting for PhotoSync.

## What It Creates

### 1. **Log Analytics Workspace**
- Centralized logging repository for all resources
- 30-day retention period
- Foundation for security monitoring and diagnostics

### 2. **Diagnostic Settings**
Automatic logging configuration for:
- **Function Apps**: All function execution logs and metrics
- **Key Vault**: Audit events (who accessed what, when)

### 3. **Security Alerts**

#### Alert: Function App Failures
- **Trigger**: More than 10 HTTP 5xx errors in 5 minutes
- **Severity**: Warning (level 2)
- **Action**: Azure Portal notification
- **Use case**: Detects application errors, potential security issues

#### Alert: Key Vault Access Failures
- **Trigger**: More than 5 failed authentication attempts (401/403) in 15 minutes
- **Severity**: High (level 1)
- **Action**: Azure Portal notification
- **Use case**: Detects unauthorized access attempts

#### Alert: Unusual File Processing Activity
- **Trigger**: More than 100 files synced in 5 minutes
- **Severity**: Warning (level 2)
- **Action**: Azure Portal notification
- **Use case**: Detects potential security breach or misconfiguration

## Usage

This module is automatically included in the main Terraform configuration:

```hcl
module "security" {
  source = "./modules/security"

  resource_prefix          = var.function_app_name_prefix
  resource_group_name      = azurerm_resource_group.photosync.name
  location                 = azurerm_resource_group.photosync.location
  function_app_source1_id  = module.function_app_source1.function_app_id
  function_app_source1_name = module.function_app_source1.function_app_name
  function_app_source2_id  = module.function_app_source2.function_app_id
  function_app_source2_name = module.function_app_source2.function_app_name
  key_vault_id             = var.enable_keyvault ? module.keyvault[0].key_vault_id : ""
}
```

## Outputs

- `log_analytics_workspace_id`: Resource ID of the workspace
- `log_analytics_workspace_name`: Name of the workspace
- `workspace_portal_url`: Direct link to workspace in Azure Portal
- `alerts_portal_url`: Direct link to view all alerts

## Viewing Logs and Alerts

### View Logs
After running `terraform apply`, get the Log Analytics workspace URL:

```bash
terraform output security_workspace_url
```

### View Alerts
Get the alerts dashboard URL:

```bash
terraform output security_alerts_url
```

### Query Logs with KQL
Example queries for security monitoring:

```kql
// Failed authentication attempts
traces
| where timestamp > ago(1h)
| where message contains "Authentication failed"
| summarize count() by bin(timestamp, 5m)

// Unusual file counts
traces
| where timestamp > ago(1h)
| where message contains "Successfully synced"
| summarize FileCount = count() by bin(timestamp, 5m)
| where FileCount > 50

// Key Vault audit events
AzureDiagnostics
| where ResourceType == "VAULTS"
| where TimeGenerated > ago(1h)
| project TimeGenerated, OperationName, CallerIPAddress, ResultType
```

## Cost

- **Log Analytics Workspace**: ~$2-5/month (first 5GB free)
- **Alerts**: ~$0.10/month per alert rule
- **Total**: ~$2.50-5.50/month

## Security Best Practices

1. **Review alerts weekly**: Check for any triggered security alerts
2. **Monitor access patterns**: Review Key Vault audit logs monthly
3. **Adjust thresholds**: Tune alert thresholds based on your usage patterns
4. **Extend retention**: For compliance, increase retention beyond 30 days

## Customization

### Adjust Alert Thresholds

Edit `main.tf` in this module to change alert thresholds:

```hcl
# Change threshold for function failures
threshold = 10  # Change to your preferred number
```

### Add Custom Alerts

Add new alert rules to `main.tf`:

```hcl
resource "azurerm_monitor_metric_alert" "custom_alert" {
  name                = "${var.resource_prefix}-custom-alert"
  resource_group_name = var.resource_group_name
  scopes              = [var.function_app_source1_id]

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "YourMetric"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 100
  }
}
```

## Integration with GitHub Actions

Security logs are separate from Application Insights (used by GitHub Actions).

- **Application Insights**: Function telemetry, performance metrics
- **Log Analytics**: Security events, audit logs, diagnostic data

Both work together for comprehensive monitoring.

## Troubleshooting

### Alert Not Triggering
1. Check if the condition threshold is met
2. Verify diagnostic settings are enabled
3. Check alert rule is enabled in Azure Portal

### No Logs Appearing
1. Wait 5-10 minutes for initial log ingestion
2. Verify Function Apps are running
3. Check diagnostic settings are correctly configured

### Permission Errors
Terraform managed identity needs:
- `Log Analytics Contributor` role
- `Monitoring Contributor` role

These are automatically granted by the Azure provider.
