# Storage Account for Function App
resource "azurerm_storage_account" "function" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
}

# App Service Plan (Consumption)
resource "azurerm_service_plan" "function" {
  name                = "${var.function_app_name}-plan"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "Y1" # Consumption plan
}

# Function App
resource "azurerm_linux_function_app" "function" {
  name                       = var.function_app_name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  service_plan_id            = azurerm_service_plan.function.id
  storage_account_name       = azurerm_storage_account.function.name
  storage_account_access_key = azurerm_storage_account.function.primary_access_key

  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = merge(
    {
      "FUNCTIONS_WORKER_RUNTIME" = "dotnet-isolated"
      "WEBSITE_RUN_FROM_PACKAGE" = "1"
      "AzureWebJobsFeatureFlags" = "EnableWorkerIndexing"
    },
    var.key_vault_url != "" ? {
      "KeyVault__VaultUrl" = var.key_vault_url
    } : {},
    var.application_insights_connection_string != "" ? {
      "APPLICATIONINSIGHTS_CONNECTION_STRING" = var.application_insights_connection_string
    } : {},
    var.source_config,
    var.destination_config
  )

  lifecycle {
    ignore_changes = [
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
    ]
  }
}
