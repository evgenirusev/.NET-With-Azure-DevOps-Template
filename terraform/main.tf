variable "environment" {
  description = "The environment type for the resources"
  type        = string
  default     = "dev"
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "example" {
  name     = "${var.environment}-example-rg-test"
  location = "West Europe"
  tags = {
    environment = var.environment
  }
}

resource "azurerm_storage_account" "example" {
  name                     = "${var.environment}examplestoragetest"
  resource_group_name      = azurerm_resource_group.example.name
  location                 = azurerm_resource_group.example.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"
  tags = {
    environment = var.environment
  }
}

resource "azurerm_key_vault" "example" {
  name                = "${var.environment}examplekeyvaulttest"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  tenant_id           = "6df5d9b1-2807-4c12-a223-63909d98a6f2"
  sku_name            = "standard"
  tags = {
    environment = var.environment
  }
}

resource "azurerm_service_plan" "example" {
  name                = "${var.environment}-service-plan-test"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  sku_name            = "B1"
  os_type             = "Windows"
  tags = {
    environment = var.environment
  }
}

resource "azurerm_windows_web_app" "example" {
  name                = "${var.environment}-app-service-test"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_service_plan.example.location
  service_plan_id     = azurerm_service_plan.example.id

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.example.instrumentation_key
  }
  
  tags = {
    environment = var.environment
  }

  site_config {}
}

resource "azurerm_application_insights" "example" {
  name                = "${var.environment}-appinsights-test"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  application_type    = "web"
  tags = {
    environment = var.environment
  }
}

resource "azurerm_mssql_server" "example" {
  name                         = "${var.environment}sqlserver-test"
  resource_group_name          = azurerm_resource_group.example.name
  location                     = azurerm_resource_group.example.location
  version                      = "12.0"
  administrator_login          = "adminuser"
  administrator_login_password = "H@Sh1CoR3!"
  tags = {
    environment = var.environment
  }
}

resource "azurerm_mssql_database" "example" {
  name      = "${var.environment}db-test"
  server_id = azurerm_mssql_server.example.id
  sku_name  = "Basic"
  tags = {
    environment = var.environment
  }
}
