output "function_app_name" {
  description = "Name of the Function App"
  value       = azurerm_function_app_flex_consumption.function.name
}

output "function_app_hostname" {
  description = "Default hostname of the Function App"
  value       = azurerm_function_app_flex_consumption.function.default_hostname
}

output "function_app_id" {
  description = "ID of the Function App"
  value       = azurerm_function_app_flex_consumption.function.id
}

output "function_app_principal_id" {
  description = "Principal ID of the Function App's managed identity"
  value       = azurerm_function_app_flex_consumption.function.identity[0].principal_id
}

output "function_app_tenant_id" {
  description = "Tenant ID of the Function App's managed identity"
  value       = azurerm_function_app_flex_consumption.function.identity[0].tenant_id
}
