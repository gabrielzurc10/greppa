# Remote state storage. Created once by scripts/bootstrap-state.sh, which rewrites
# this file with the generated storage account name.
terraform {
  backend "azurerm" {
    resource_group_name  = "greppa-tfstate-rg"
    storage_account_name = "REPLACED_BY_BOOTSTRAP"
    container_name       = "tfstate"
    key                  = "greppa.tfstate"
  }
}
