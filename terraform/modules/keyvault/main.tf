# Azure Key Vault for storing refresh tokens and secrets

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "photosync" {
  name                        = var.key_vault_name
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = false
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false
  sku_name                    = "standard"

  network_acls {
    bypass         = "AzureServices"
    default_action = "Allow"
  }

  tags = var.tags
}

# Grant the current user (running Terraform) access to manage secrets
resource "azurerm_key_vault_access_policy" "terraform_user" {
  key_vault_id = azurerm_key_vault.photosync.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Purge",
    "Recover"
  ]
}

# Store refresh tokens as secrets (if provided)
resource "azurerm_key_vault_secret" "source1_refresh_token" {
  count = var.source1_refresh_token != null ? 1 : 0

  name         = "source1-refresh-token"
  value        = var.source1_refresh_token
  key_vault_id = azurerm_key_vault.photosync.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "source2_refresh_token" {
  count = var.source2_refresh_token != null ? 1 : 0

  name         = "source2-refresh-token"
  value        = var.source2_refresh_token
  key_vault_id = azurerm_key_vault.photosync.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "destination_refresh_token" {
  count = var.destination_refresh_token != null ? 1 : 0

  name         = "destination-refresh-token"
  value        = var.destination_refresh_token
  key_vault_id = azurerm_key_vault.photosync.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

# Store client secrets (if provided, as alternative to refresh tokens)
resource "azurerm_key_vault_secret" "source1_client_secret" {
  count = var.source1_client_secret != null ? 1 : 0

  name         = "source1-client-secret"
  value        = var.source1_client_secret
  key_vault_id = azurerm_key_vault.photosync.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "source2_client_secret" {
  count = var.source2_client_secret != null ? 1 : 0

  name         = "source2-client-secret"
  value        = var.source2_client_secret
  key_vault_id = azurerm_key_vault.photosync.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}

resource "azurerm_key_vault_secret" "destination_client_secret" {
  count = var.destination_client_secret != null ? 1 : 0

  name         = "destination-client-secret"
  value        = var.destination_client_secret
  key_vault_id = azurerm_key_vault.photosync.id

  depends_on = [azurerm_key_vault_access_policy.terraform_user]
}
