output "resource_group_name" {
  description = "Name of the created resource group"
  value       = azurerm_resource_group.photosync.name
}

output "function_app_source1_name" {
  description = "Name of the Function App for source 1"
  value       = module.function_app_source1.function_app_name
}

output "function_app_source1_url" {
  description = "URL of the Function App for source 1"
  value       = "https://${module.function_app_source1.function_app_hostname}"
}

output "function_app_source2_name" {
  description = "Name of the Function App for source 2"
  value       = module.function_app_source2.function_app_name
}

output "function_app_source2_url" {
  description = "URL of the Function App for source 2"
  value       = "https://${module.function_app_source2.function_app_hostname}"
}

output "deployment_commands" {
  description = "Commands to deploy the function code to both apps"
  value = <<-EOT
    # Deploy to source 1:
    func azure functionapp publish ${module.function_app_source1.function_app_name}

    # Deploy to source 2:
    func azure functionapp publish ${module.function_app_source2.function_app_name}
  EOT
}

output "key_vault_name" {
  description = "Name of the Key Vault (if enabled)"
  value       = var.enable_keyvault ? module.keyvault[0].key_vault_name : null
}

output "key_vault_uri" {
  description = "URI of the Key Vault (if enabled)"
  value       = var.enable_keyvault ? module.keyvault[0].key_vault_uri : null
}

output "setup_instructions" {
  description = "Next steps for completing setup"
  value       = var.enable_keyvault ? "See Key Vault setup instructions in docs/PERSONAL_ACCOUNTS_SETUP.md" : "Standard deployment - see README.md"
}

output "application_insights_name" {
  description = "Name of the Application Insights instance"
  value       = module.application_insights.name
}

output "application_insights_app_id" {
  description = "Application Insights application ID for querying logs"
  value       = module.application_insights.app_id
}

output "application_insights_connection_string" {
  description = "Application Insights connection string (sensitive)"
  value       = module.application_insights.connection_string
  sensitive   = true
}

output "logs_portal_url" {
  description = "URL to view logs in Azure Portal"
  value       = "https://portal.azure.com/#@/resource${module.application_insights.id}/logs"
}

# Security monitoring outputs
output "security_workspace_url" {
  description = "URL to view Log Analytics workspace (used by App Insights) in Azure Portal"
  value       = module.security.workspace_portal_url
}

output "security_alerts_url" {
  description = "URL to view security alerts in Azure Portal"
  value       = module.security.alerts_portal_url
}

# OneDrive App Registration outputs
output "onedrive_app_client_id" {
  description = "Client ID of the OneDrive App Registration (use with get-refresh-token.js)"
  value       = azuread_application.photosync_onedrive.client_id
}

output "onedrive_app_client_secret" {
  description = "Client Secret of the OneDrive App Registration (use with get-refresh-token.js)"
  value       = azuread_application_password.photosync_onedrive.value
  sensitive   = true
}

output "refresh_token_command" {
  description = "Command to generate refresh tokens for OneDrive accounts; copy and run this in a POSIX-compatible shell (e.g., bash/zsh) that supports $(...) command substitution so the nested 'terraform output' is evaluated"
  value       = "node tools/get-refresh-token.js ${azuread_application.photosync_onedrive.client_id} $(terraform output -raw onedrive_app_client_secret)"
}
