# Security Module Variables

variable "resource_prefix" {
  description = "Prefix for resource names"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region for resources"
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace (from Application Insights)"
  type        = string
}

variable "function_app_source1_id" {
  description = "Resource ID of Function App Source 1"
  type        = string
}

variable "function_app_source1_name" {
  description = "Name of Function App Source 1"
  type        = string
}

variable "function_app_source2_id" {
  description = "Resource ID of Function App Source 2"
  type        = string
}

variable "function_app_source2_name" {
  description = "Name of Function App Source 2"
  type        = string
}

variable "key_vault_id" {
  description = "Resource ID of Key Vault (empty string if not enabled)"
  type        = string
  default     = ""
}
