resource "azurerm_static_web_app" "frontend" {
  name                = "${var.prefix}-frontend"
  location            = var.swa_location
  resource_group_name = data.azurerm_resource_group.main.name
  sku_tier            = "Free"
  sku_size            = "Free"
}
