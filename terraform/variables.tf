variable "resource_group_name" {
  description = "Existing resource group that hosts all app resources (never created or destroyed by Terraform)."
  type        = string
  default     = "greppa-rg"
}

variable "location" {
  description = "Region for the backend resources. Defaults to the resource group's own region at plan time via the data source; override only if needed."
  type        = string
  default     = "eastus"
}

variable "swa_location" {
  description = "Region for the Static Web App. The Free tier is only offered in a few regions (westus2, centralus, eastus2, westeurope, eastasia)."
  type        = string
  default     = "eastus2"
}

variable "prefix" {
  description = "Name prefix for all resources."
  type        = string
  default     = "greppa"
}

variable "image_tag" {
  description = "Tag of the backend container image in ACR (the git SHA in CI)."
  type        = string
}

variable "openai_api_key" {
  description = "OpenAI API key, injected as a Container App secret."
  type        = string
  sensitive   = true
}

variable "semgrep_app_token" {
  description = "Semgrep app token, injected as a Container App secret."
  type        = string
  sensitive   = true
}
