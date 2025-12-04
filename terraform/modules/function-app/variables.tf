variable "function_app_name" {
  description = "Name of the Azure Function App"
  type        = string
}

variable "storage_account_name" {
  description = "Name of the storage account for the Function App"
  type        = string

  validation {
    condition     = length(var.storage_account_name) >= 3 && length(var.storage_account_name) <= 24 && can(regex("^[a-z0-9]+$", var.storage_account_name))
    error_message = "Storage account name must be lowercase alphanumeric, between 3 and 24 characters."
  }
}

variable "resource_group_name" {
  description = "Name of the Azure Resource Group"
  type        = string
}

variable "location" {
  description = "Azure region for resources"
  type        = string
}

variable "source_config" {
  description = "Source OneDrive configuration settings"
  type        = map(string)
  sensitive   = true
}

variable "destination_config" {
  description = "Destination OneDrive configuration settings"
  type        = map(string)
  sensitive   = true
}
