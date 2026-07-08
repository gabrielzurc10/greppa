resource "azurerm_container_app_environment" "main" {
  name                       = "${var.prefix}-cae"
  location                   = var.location
  resource_group_name        = data.azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}

resource "azurerm_container_app" "backend" {
  name                         = "${var.prefix}-backend"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = data.azurerm_resource_group.main.name
  revision_mode                = "Single"

  template {
    # max_replicas = 1 is required: the job store is in-memory, so polling must
    # always land on the replica that ran the scan.
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "backend"
      image  = "${azurerm_container_registry.main.login_server}/greppa-backend:${var.image_tag}"
      cpu    = 1.0
      memory = "2Gi"

      env {
        name        = "OPENAI_API_KEY"
        secret_name = "openai-api-key"
      }

      env {
        name        = "SEMGREP_APP_TOKEN"
        secret_name = "semgrep-app-token"
      }

      env {
        name  = "Cors__AllowedOrigin"
        value = "https://${azurerm_static_web_app.frontend.default_host_name}"
      }

      liveness_probe {
        path      = "/healthz"
        port      = 8080
        transport = "HTTP"
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  secret {
    name  = "openai-api-key"
    value = var.openai_api_key
  }

  secret {
    name  = "semgrep-app-token"
    value = var.semgrep_app_token
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }
}
