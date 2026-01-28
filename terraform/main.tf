terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.55"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

provider "azuread" {
  # Uses the same authentication as azurerm
}

# Data source to get current Azure AD tenant
data "azuread_client_config" "current" {}

# Azure AD Application Registration for OneDrive access
resource "azuread_application" "photosync_onedrive" {
  display_name     = "${var.function_app_name_prefix}-onedrive"
  sign_in_audience = "AzureADandPersonalMicrosoftAccount"
  owners           = [data.azuread_client_config.current.object_id]

  api {
    requested_access_token_version = 2
  }

  web {
    redirect_uris = ["http://localhost:8080/callback"]
  }

  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    # Files.Read - Delegated
    resource_access {
      id   = "10465720-29dd-4523-a11a-6a75c743c9d9"
      type = "Scope"
    }

    # Files.ReadWrite - Delegated
    resource_access {
      id   = "5c28f0bf-8a70-41f1-8ab2-9032436ddb65"
      type = "Scope"
    }

    # offline_access - Delegated
    resource_access {
      id   = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"
      type = "Scope"
    }
  }
}

# Client secret for the OneDrive application
resource "azuread_application_password" "photosync_onedrive" {
  application_id = azuread_application.photosync_onedrive.id
  display_name   = "Terraform managed secret"

  depends_on = [azuread_application.photosync_onedrive]

  lifecycle {
    ignore_changes = [end_date]
  }
}

# Local values for the generated credentials
locals {
  onedrive_client_id     = azuread_application.photosync_onedrive.client_id
  onedrive_client_secret = azuread_application_password.photosync_onedrive.value
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
  # Override ClientId with the Terraform-managed App Registration
  source_config = merge(
    {
      for key, value in var.onedrive1_config :
      replace(replace(key, "OneDrive1:", "OneDriveSource__"), ":", "__") => value
    },
    {
      "OneDriveSource__ClientId" = local.onedrive_client_id
    }
  )

  destination_config = merge(
    {
      for key, value in var.onedrive_destination_config :
      replace(key, ":", "__") => value
    },
    {
      "OneDriveDestination__ClientId" = local.onedrive_client_id
    }
  )
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
  # Override ClientId with the Terraform-managed App Registration
  source_config = merge(
    {
      for key, value in var.onedrive2_config :
      replace(replace(key, "OneDrive2:", "OneDriveSource__"), ":", "__") => value
    },
    {
      "OneDriveSource__ClientId" = local.onedrive_client_id
    }
  )

  destination_config = merge(
    {
      for key, value in var.onedrive_destination_config :
      replace(key, ":", "__") => value
    },
    {
      "OneDriveDestination__ClientId" = local.onedrive_client_id
    }
  )
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

  # Client secret from the Terraform-managed App Registration (same for all sources)
  source1_client_secret      = local.onedrive_client_secret
  source2_client_secret      = local.onedrive_client_secret
  destination_client_secret  = local.onedrive_client_secret

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
  application_insights_id     = module.application_insights.id
  function_app_source1_id     = module.function_app_source1.function_app_id
  function_app_source1_name   = module.function_app_source1.function_app_name
  function_app_source2_id     = module.function_app_source2.function_app_id
  function_app_source2_name   = module.function_app_source2.function_app_name
  key_vault_id                = var.enable_keyvault ? module.keyvault[0].key_vault_id : ""
  enable_keyvault             = var.enable_keyvault
  alert_email                 = var.alert_email
}
