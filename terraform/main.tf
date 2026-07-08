# The resource group already exists and is intentionally NOT managed by Terraform:
# `terraform destroy` empties it but never deletes the group itself.
data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}
