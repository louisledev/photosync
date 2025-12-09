# Security Module Outputs

output "workspace_portal_url" {
  description = "URL to view Log Analytics workspace in Azure Portal"
  value       = "https://portal.azure.com/#resource${var.log_analytics_workspace_id}/overview"
}

output "alerts_portal_url" {
  description = "URL to view alerts in Azure Portal"
  value       = "https://portal.azure.com/#blade/Microsoft_Azure_Monitoring/AzureMonitoringBrowseBlade/alertsV2"
}
