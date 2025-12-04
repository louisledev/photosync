# Azure Backend Configuration for Terraform State
#
# This configures Terraform to store state in Azure Storage instead of locally.
# This allows multiple team members to work on the infrastructure and ensures
# the state is securely stored and backed up.
#
# IMPORTANT: Before using this backend, you must:
# 1. Create the Azure Storage account and container (see TERRAFORM.md for setup script)
# 2. Uncomment the backend configuration below
# 3. Run `terraform init -migrate-state` to move existing state to Azure

# Uncomment the backend block below after creating the storage account:
terraform {
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "photosyncterraformstate"  # Must be globally unique
    container_name       = "tfstate"
    key                  = "photosync.terraform.tfstate"
  }
}

