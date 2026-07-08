terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

# Credentials come from ARM_* environment variables:
# locally via `az login` + ARM_SUBSCRIPTION_ID, in CI via OIDC (ARM_USE_OIDC=true).
provider "azurerm" {
  features {}

  # CI runs as a service principal with RG-scoped Contributor only; it cannot
  # register resource providers at the subscription level. The providers this
  # stack needs are registered once, manually (see README).
  resource_provider_registrations = "none"
}
