resource "azurerm_storage_container" "primary" {
	name 					= "tfa-${var.prefix}-vhds"
	resource_group_name 	= "${var.storage_rg_name}"
	storage_account_name 	= "${var.storage_acct_name}"
	container_access_type 	= "private"
}
