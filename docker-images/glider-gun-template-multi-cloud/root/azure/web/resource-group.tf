resource "azurerm_resource_group" "primary" {
	name 					= "tfa-${var.prefix}-resources"
	location 				= "${var.region_name}"
}
