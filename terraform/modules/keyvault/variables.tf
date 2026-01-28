variable "key_vault_name" {
  description = "Name of the Azure Key Vault (must be globally unique, 3-24 chars)"
  type        = string
}

variable "location" {
  description = "Azure region for Key Vault"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}

# Refresh tokens (for personal Microsoft accounts)
variable "source1_refresh_token" {
  description = "OAuth refresh token for source 1 OneDrive account"
  type        = string
  sensitive   = true
  default     = null
}

variable "source2_refresh_token" {
  description = "OAuth refresh token for source 2 OneDrive account"
  type        = string
  sensitive   = true
  default     = null
}

variable "destination_refresh_token" {
  description = "OAuth refresh token for destination OneDrive account"
  type        = string
  sensitive   = true
  default     = null
}

# Client secrets (managed by Terraform via App Registration)
variable "source1_client_secret" {
  description = "Azure AD app client secret for source 1"
  type        = string
  sensitive   = true
}

variable "source2_client_secret" {
  description = "Azure AD app client secret for source 2"
  type        = string
  sensitive   = true
}

variable "destination_client_secret" {
  description = "Azure AD app client secret for destination"
  type        = string
  sensitive   = true
}
