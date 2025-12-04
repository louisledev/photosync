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
  value       = var.enable_keyvault ? "See Key Vault setup instructions in PERSONAL_ACCOUNTS_SETUP.md" : "Standard deployment - see README.md"
}
