# Application Insights for monitoring Function Apps
resource "azurerm_application_insights" "photosync" {
  name                = var.application_insights_name
  location            = var.location
  resource_group_name = var.resource_group_name
  application_type    = "web"
  retention_in_days   = 30  # Keep logs for 30 days (minimum allowed, valid values: 30, 60, 90, 120, 180, 270, 365, 550, 730)

  tags = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "PhotoSync"
  }

  lifecycle {
    # Ignore workspace_id changes as it's automatically set by Azure during creation.
    # This prevents Terraform from attempting unnecessary updates to this computed field.
    ignore_changes = [
      workspace_id,
    ]
  }
}
