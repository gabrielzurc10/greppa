# ACR names must be globally unique and alphanumeric; derive a stable suffix from
# the resource group id so the name never changes across applies.
resource "azurerm_container_registry" "main" {
  name                = "${var.prefix}acr${substr(md5(data.azurerm_resource_group.main.id), 0, 8)}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  sku                 = "Basic"

  # Admin credentials keep the CI principal at plain Contributor (no role-assignment
  # writes). Production-grade alternative: managed identity + AcrPull.
  admin_enabled = true
}
