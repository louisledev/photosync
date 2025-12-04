output "function_app_name" {
  description = "Name of the Function App"
  value       = azurerm_linux_function_app.function.name
}

output "function_app_hostname" {
  description = "Default hostname of the Function App"
  value       = azurerm_linux_function_app.function.default_hostname
}

output "function_app_id" {
  description = "ID of the Function App"
  value       = azurerm_linux_function_app.function.id
}

output "function_app_principal_id" {
  description = "Principal ID of the Function App's managed identity"
  value       = azurerm_linux_function_app.function.identity[0].principal_id
}

output "function_app_tenant_id" {
  description = "Tenant ID of the Function App's managed identity"
  value       = azurerm_linux_function_app.function.identity[0].tenant_id
}
