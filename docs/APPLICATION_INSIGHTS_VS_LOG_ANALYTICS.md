# Application Insights vs Log Analytics: Understanding the Relationship

## TL;DR

**You don't need a separate Log Analytics workspace!** Application Insights already creates one behind the scenes. The security module **reuses** this existing workspace to avoid duplication and extra cost.

## The Confusion

When setting up Azure monitoring, it's easy to think you need:
- Application Insights (for app logs)
- PLUS a separate Log Analytics workspace (for security logs)

**This is NOT true!** They work together, not separately.

## How It Actually Works

```
┌─────────────────────────────────────────────────────┐
│  Application Insights                               │
│  (Your app's entry point for logging)               │
│    ↓ Sends data to                                  │
│                                                      │
│  ┌───────────────────────────────────────────────┐  │
│  │  Log Analytics Workspace (auto-created)       │  │
│  │                                                │  │
│  │  Tables:                                       │  │
│  │  - traces (your LogInformation calls)          │  │
│  │  - exceptions (your LogError calls)            │  │
│  │  - requests (HTTP requests)                    │  │
│  │  - dependencies (external API calls)           │  │
│  │  - customMetrics (performance counters)        │  │
│  │  - AzureDiagnostics (Key Vault audit logs)     │  │
│  │  - FunctionAppLogs (platform diagnostics)      │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

## What Each Provides

### Application Insights
- **User-friendly interface** for developers
- **Smart defaults** for .NET applications
- **Application map** showing dependencies
- **Failures analysis** with stack traces
- **Performance profiler** for optimization
- Automatic correlation of logs

### Log Analytics Workspace
- **Backend storage** for all the data
- **KQL query engine** for custom queries
- **Long-term retention** configuration
- **Cross-resource queries** (combine multiple apps)
- **Security and compliance** views

## Why Reuse the Workspace?

### ✅ Benefits of Reusing

1. **Single source of truth**: All logs in one place
2. **Cost savings**: Only pay for one workspace
3. **Simpler queries**: No need to join across workspaces
4. **Better correlation**: App logs + infrastructure logs together
5. **Easier retention management**: One retention policy

### ❌ Why NOT Create a Separate Workspace

1. **Extra cost**: ~$2-5/month per workspace
2. **Data duplication**: Same data in multiple places
3. **Complex queries**: Need to query multiple workspaces
4. **Harder to correlate**: App events separated from infrastructure events

## How PhotoSync Uses This

### Application Insights (already configured)
```hcl
resource "azurerm_application_insights" "photosync" {
  name                = "photosync-insights"
  # ... other config
  # workspace_id is automatically created by Azure!
}
```

### Security Module (reuses the workspace)
```hcl
module "security" {
  source = "./modules/security"

  # Pass the workspace ID from App Insights
  log_analytics_workspace_id = module.application_insights.workspace_id

  # Configure diagnostic settings to send logs to same workspace
  # ...
}
```

## What Data Goes Where

| Data Type | Sent By | Table in Workspace | Viewable In |
|-----------|---------|-------------------|-------------|
| Your C# logs | `_logger.LogInformation()` | `traces` | App Insights → Logs |
| Exceptions | `_logger.LogError()` | `exceptions` | App Insights → Failures |
| HTTP requests | Function runtime | `requests` | App Insights → Performance |
| Function diagnostics | Azure platform | `FunctionAppLogs` | Log Analytics → Logs |
| Key Vault audit | Azure platform | `AzureDiagnostics` | Log Analytics → Logs |
| Security alerts | Azure Monitor | `Alert` | Azure Monitor → Alerts |

## Example Queries

All these queries work in the **same workspace**!

### Query App Logs
```kql
traces
| where timestamp > ago(1h)
| where message contains "Successfully synced"
```

### Query Key Vault Access
```kql
AzureDiagnostics
| where ResourceType == "VAULTS"
| where TimeGenerated > ago(1h)
| project TimeGenerated, OperationName, CallerIPAddress
```

### Cross-Query (App + Infrastructure)
```kql
// Find app errors that happened near Key Vault failures
let keyVaultErrors = AzureDiagnostics
    | where ResourceType == "VAULTS"
    | where ResultType != "Success";
let appErrors = exceptions
    | where timestamp > ago(1h);
keyVaultErrors
| join kind=inner appErrors on $left.TimeGenerated == $right.timestamp
```

## Viewing Your Data

### Option 1: Application Insights Portal (Developer-Friendly)
```bash
terraform output logs_portal_url
```
- Shows: App logs, exceptions, performance, dependencies
- Best for: Debugging app issues, performance analysis

### Option 2: Log Analytics Workspace (Security-Focused)
```bash
terraform output security_workspace_url
```
- Shows: All logs including infrastructure and security
- Best for: Security audits, compliance, cross-resource queries

### Option 3: Azure Monitor Alerts
```bash
terraform output security_alerts_url
```
- Shows: Active alerts and alert history
- Best for: Responding to incidents

## Cost Breakdown

| Component | Cost | Notes |
|-----------|------|-------|
| Application Insights | $0 | First 5GB/month free |
| Log Analytics Workspace | $0 | First 5GB/month free (shared with App Insights) |
| Data ingestion (over 5GB) | ~$2.30/GB | Both use same quota |
| Data retention (30 days) | Included | Free for first 31 days |
| Alert rules | ~$0.10/rule/month | 3 rules = $0.30/month |
| **Total (typical)** | **~$0.30-2/month** | For ~500 photos/month |

## Summary

✅ **One workspace, two views**:
- Application Insights = Developer view
- Log Analytics = Security/ops view

✅ **PhotoSync configuration**:
- App Insights creates workspace automatically
- Security module reuses that workspace
- All logs stored in one place

✅ **Benefits**:
- Lower cost (one workspace)
- Simpler queries (all data together)
- Better correlation (app + infrastructure)

✅ **No action needed**:
- Just run `terraform apply`
- Everything is configured automatically
