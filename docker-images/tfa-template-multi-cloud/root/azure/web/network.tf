resource "azurerm_virtual_network" "primary" {
	name 						= "tfa-${var.prefix}-network"
	address_space 				= ["10.7.0.0/16"]
	location 					= "${var.region_name}"
	resource_group_name			= "${azurerm_resource_group.primary.name}"
}

resource "azurerm_subnet" "primary" {
	name 						= "tfa-${var.prefix}-subnet"
	resource_group_name			= "${azurerm_resource_group.primary.name}"
	virtual_network_name 		= "${azurerm_virtual_network.primary.name}"
	address_prefix 				= "10.7.1.0/24"
}

resource "azurerm_network_interface" "primary" {
	count 						= "${var.instance_count}"

	name 						= "azure-${var.prefix}-web-${format("%02d", count.index + 1)}-ni"
	location 					= "${var.region_name}"
	resource_group_name 		= "${azurerm_resource_group.primary.name}"

	ip_configuration {
		name 					= "azure-${var.prefix}-web-${format("%02d", count.index + 1)}-ni-config"
		subnet_id 				= "${azurerm_subnet.primary.id}"

		# Hook up public IP to private IP.
		public_ip_address_id	= "${element(azurerm_public_ip.primary.*.id, count.index)}"
		
		private_ip_address_allocation = "dynamic"
	}
}

resource "azurerm_public_ip" "primary" {
	count 					= "${var.instance_count}"

	name 					= "tfa-${var.prefix}-web-${format("%02d", count.index + 1)}-pip"
	location 				= "${var.region_name}"
	resource_group_name 	= "${azurerm_resource_group.primary.name}"

	public_ip_address_allocation = "static"
}
