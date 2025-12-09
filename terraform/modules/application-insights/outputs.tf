output "instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = azurerm_application_insights.photosync.instrumentation_key
  sensitive   = true
}

output "connection_string" {
  description = "Application Insights connection string"
  value       = azurerm_application_insights.photosync.connection_string
  sensitive   = true
}

output "app_id" {
  description = "Application Insights application ID"
  value       = azurerm_application_insights.photosync.app_id
}

output "id" {
  description = "Application Insights resource ID"
  value       = azurerm_application_insights.photosync.id
}

output "name" {
  description = "Application Insights name"
  value       = azurerm_application_insights.photosync.name
}

output "workspace_id" {
  description = "ID of the underlying Log Analytics workspace"
  value       = azurerm_application_insights.photosync.workspace_id
}
