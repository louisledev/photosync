terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.55"
    }
  }
}

provider "azurerm" {
  features {}
}

# Resource Group (shared by both Function Apps)
resource "azurerm_resource_group" "photosync" {
  name     = var.resource_group_name
  location = var.location
}

# Application Insights for monitoring
module "application_insights" {
  source = "./modules/application-insights"

  application_insights_name = "${var.function_app_name_prefix}-insights"
  resource_group_name       = azurerm_resource_group.photosync.name
  location                  = azurerm_resource_group.photosync.location
}

# Function App for OneDrive Source 1
module "function_app_source1" {
  source = "./modules/function-app"

  function_app_name    = "${var.function_app_name_prefix}-source1"
  storage_account_name = "${var.storage_account_name_prefix}src1"
  resource_group_name  = azurerm_resource_group.photosync.name
  location             = azurerm_resource_group.photosync.location

  # Pass Key Vault URL if Key Vault is enabled
  key_vault_url = var.enable_keyvault ? module.keyvault[0].key_vault_uri : ""

  # Pass Application Insights connection string
  application_insights_connection_string = module.application_insights.connection_string

  # Rename OneDrive1 config to OneDriveSource for this Function App
  # Replace colons with double underscores for Azure App Settings
  source_config = {
    for key, value in var.onedrive1_config :
    replace(replace(key, "OneDrive1:", "OneDriveSource__"), ":", "__") => value
  }

  destination_config = {
    for key, value in var.onedrive_destination_config :
    replace(key, ":", "__") => value
  }
}

# Function App for OneDrive Source 2
module "function_app_source2" {
  source = "./modules/function-app"

  function_app_name    = "${var.function_app_name_prefix}-source2"
  storage_account_name = "${var.storage_account_name_prefix}src2"
  resource_group_name  = azurerm_resource_group.photosync.name
  location             = azurerm_resource_group.photosync.location

  # Pass Key Vault URL if Key Vault is enabled
  key_vault_url = var.enable_keyvault ? module.keyvault[0].key_vault_uri : ""

  # Pass Application Insights connection string
  application_insights_connection_string = module.application_insights.connection_string

  # Rename OneDrive2 config to OneDriveSource for this Function App
  # Replace colons with double underscores for Azure App Settings
  source_config = {
    for key, value in var.onedrive2_config :
    replace(replace(key, "OneDrive2:", "OneDriveSource__"), ":", "__") => value
  }

  destination_config = {
    for key, value in var.onedrive_destination_config :
    replace(key, ":", "__") => value
  }
}

# Azure Key Vault for storing refresh tokens and secrets
module "keyvault" {
  source = "./modules/keyvault"
  count  = var.enable_keyvault ? 1 : 0

  key_vault_name       = var.key_vault_name
  resource_group_name  = azurerm_resource_group.photosync.name
  location             = azurerm_resource_group.photosync.location

  # Refresh tokens (for personal Microsoft accounts)
  source1_refresh_token      = var.source1_refresh_token
  source2_refresh_token      = var.source2_refresh_token
  destination_refresh_token  = var.destination_refresh_token

  # Client secrets (optional, for fallback)
  source1_client_secret      = var.source1_client_secret_for_vault
  source2_client_secret      = var.source2_client_secret_for_vault
  destination_client_secret  = var.destination_client_secret_for_vault

  tags = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "PhotoSync"
  }
}

# Grant Function App 1 access to Key Vault
resource "azurerm_key_vault_access_policy" "function_app_source1" {
  count = var.enable_keyvault ? 1 : 0

  key_vault_id = module.keyvault[0].key_vault_id
  tenant_id    = module.function_app_source1.function_app_tenant_id
  object_id    = module.function_app_source1.function_app_principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

# Grant Function App 2 access to Key Vault
resource "azurerm_key_vault_access_policy" "function_app_source2" {
  count = var.enable_keyvault ? 1 : 0

  key_vault_id = module.keyvault[0].key_vault_id
  tenant_id    = module.function_app_source2.function_app_tenant_id
  object_id    = module.function_app_source2.function_app_principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

# Security monitoring and alerts
module "security" {
  source = "./modules/security"

  resource_prefix             = var.function_app_name_prefix
  resource_group_name         = azurerm_resource_group.photosync.name
  location                    = azurerm_resource_group.photosync.location
  log_analytics_workspace_id  = module.application_insights.workspace_id
  function_app_source1_id     = module.function_app_source1.function_app_id
  function_app_source1_name   = module.function_app_source1.function_app_name
  function_app_source2_id     = module.function_app_source2.function_app_id
  function_app_source2_name   = module.function_app_source2.function_app_name
  key_vault_id                = var.enable_keyvault ? module.keyvault[0].key_vault_id : ""
}
