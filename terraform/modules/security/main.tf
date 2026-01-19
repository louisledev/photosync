# Security Module - Configures security monitoring and alerts for PhotoSync
#
# NOTE: Most resources are disabled due to permission issues with managed Log Analytics
# workspaces and multi-resource alert limitations. These can be enabled manually via
# Azure Portal after deployment if needed.

# Diagnostic settings for Function App 1
# DISABLED: Requires permissions on managed Log Analytics workspace
resource "azurerm_monitor_diagnostic_setting" "function_app_source1" {
  count                      = 0 # Disabled - permission issue with managed workspace
  name                       = "${var.function_app_source1_name}-diagnostics"
  target_resource_id         = var.function_app_source1_id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "FunctionAppLogs"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}

# Diagnostic settings for Function App 2
# DISABLED: Requires permissions on managed Log Analytics workspace
resource "azurerm_monitor_diagnostic_setting" "function_app_source2" {
  count                      = 0 # Disabled - permission issue with managed workspace
  name                       = "${var.function_app_source2_name}-diagnostics"
  target_resource_id         = var.function_app_source2_id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "FunctionAppLogs"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}

# Diagnostic settings for Key Vault (if enabled)
# DISABLED: Requires permissions on managed Log Analytics workspace
resource "azurerm_monitor_diagnostic_setting" "keyvault" {
  count                      = 0 # Disabled - permission issue with managed workspace
  name                       = "keyvault-diagnostics"
  target_resource_id         = var.key_vault_id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  enabled_log {
    category = "AuditEvent"
  }

  enabled_metric {
    category = "AllMetrics"
  }
}

# Alert for Function App failures
# DISABLED: Multi-resource alerts not supported for Function Apps
resource "azurerm_monitor_metric_alert" "function_app_failures" {
  count               = 0 # Disabled - multi-resource alerts not supported for Function Apps
  name                = "${var.resource_prefix}-function-failures"
  resource_group_name = var.resource_group_name
  scopes              = [var.function_app_source1_id, var.function_app_source2_id]
  description         = "Alert when Function Apps have more than 10 HTTP 5xx errors in 5 minutes"
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 10
  }

  tags = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "PhotoSync"
  }
}

# Alert for Key Vault access failures (if enabled)
resource "azurerm_monitor_metric_alert" "keyvault_failures" {
  count               = var.enable_keyvault ? 1 : 0
  name                = "${var.resource_prefix}-keyvault-failures"
  resource_group_name = var.resource_group_name
  scopes              = [var.key_vault_id]
  description         = "Alert when Key Vault has authentication failures"
  severity            = 1
  frequency           = "PT5M"
  window_size         = "PT15M"

  criteria {
    metric_namespace = "Microsoft.KeyVault/vaults"
    metric_name      = "ServiceApiResult"
    aggregation      = "Count"
    operator         = "GreaterThan"
    threshold        = 5

    dimension {
      name     = "StatusCode"
      operator = "Include"
      values   = ["401", "403"]
    }
  }

  tags = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "PhotoSync"
  }
}

# Alert for unusual number of processed files (potential security issue)
# DISABLED: Log Analytics workspace needs data before this can work
resource "azurerm_monitor_scheduled_query_rules_alert_v2" "unusual_activity" {
  count                = 0 # Disabled on initial deployment - enable after first run
  name                 = "${var.resource_prefix}-unusual-activity"
  resource_group_name  = var.resource_group_name
  location             = var.location
  scopes               = [var.log_analytics_workspace_id]
  severity             = 2
  evaluation_frequency = "PT5M"
  window_duration      = "PT15M"
  description          = "Alert when PhotoSync processes an unusual number of files"

  criteria {
    query                   = <<-QUERY
      traces
      | where timestamp > ago(15m)
      | where message contains "Successfully synced"
      | summarize SyncCount = count() by bin(timestamp, 5m)
      | where SyncCount > 100
    QUERY
    time_aggregation_method = "Count"
    threshold               = 0
    operator                = "GreaterThan"

    failing_periods {
      minimum_failing_periods_to_trigger_alert = 1
      number_of_evaluation_periods             = 1
    }
  }

  tags = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "PhotoSync"
  }
}
