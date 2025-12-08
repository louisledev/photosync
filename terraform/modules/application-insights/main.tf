# Application Insights for monitoring Function Apps
resource "azurerm_application_insights" "photosync" {
  name                = var.application_insights_name
  location            = var.location
  resource_group_name = var.resource_group_name
  application_type    = "web"

  tags = {
    Environment = "Production"
    ManagedBy   = "Terraform"
    Project     = "PhotoSync"
  }
}
