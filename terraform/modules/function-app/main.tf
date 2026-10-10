# Storage Account for Function App
resource "azurerm_storage_account" "function" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
}

# Azure cannot change an existing Y1 plan to FC1. Use a distinct name so
# Terraform replaces the legacy plan instead of attempting a SKU update.
resource "azurerm_service_plan" "function" {
  name                = "${var.function_app_name}-flex-plan"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "FC1" # Flex Consumption plan

  lifecycle {
    create_before_destroy = true
  }
}

# Private blob container used by Flex Consumption OneDeploy.
resource "azurerm_storage_container" "deployment" {
  name                  = "function-releases"
  storage_account_id    = azurerm_storage_account.function.id
  container_access_type = "private"
}

# Existing Consumption apps must be migrated before adopting this resource type.
# See .github/DEPLOYMENT.md for migration and Terraform state reconciliation.
resource "azurerm_function_app_flex_consumption" "function" {
  name                = var.function_app_name
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.function.id

  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.function.primary_blob_endpoint}${azurerm_storage_container.deployment.name}"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.function.primary_access_key
  runtime_name                = "dotnet-isolated"
  runtime_version             = "10.0"
  instance_memory_in_mb       = 2048
  maximum_instance_count      = 40

  site_config {
    application_insights_connection_string = var.application_insights_connection_string != "" ? var.application_insights_connection_string : null
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = merge(
    {
      "AzureWebJobsStorage" = azurerm_storage_account.function.primary_connection_string
    },
    var.key_vault_url != "" ? {
      # Double underscores are used here to represent colons in .NET configuration keys.
      # This follows .NET conventions, as Terraform does not support colons in keys.
      "KeyVault__VaultUrl" = var.key_vault_url
    } : {},
    var.source_config,
    var.destination_config
  )
}
