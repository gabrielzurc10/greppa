output "acr_name" {
  value = azurerm_container_registry.main.name
}

output "acr_login_server" {
  value = azurerm_container_registry.main.login_server
}

output "backend_url" {
  value = "https://${azurerm_container_app.backend.ingress[0].fqdn}"
}

output "swa_hostname" {
  value = azurerm_static_web_app.frontend.default_host_name
}

# SWA deployment token, consumed by the deploy workflow. It lives in Terraform
# state — the state storage account must stay private.
output "swa_api_key" {
  value     = azurerm_static_web_app.frontend.api_key
  sensitive = true
}
