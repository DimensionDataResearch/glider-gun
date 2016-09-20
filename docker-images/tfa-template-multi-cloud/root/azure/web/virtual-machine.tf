resource "azurerm_virtual_machine" "web" {
	count 					= "${var.instance_count}"

	name 					= "azure-${var.prefix}-web-${format("%02d", count.index + 1)}"
	location 				= "${var.region_name}" # West US
	resource_group_name 	= "${azurerm_resource_group.primary.name}"
	network_interface_ids 	= ["${element(azurerm_network_interface.primary.*.id, count.index)}"]

	vm_size 				= "${var.instance_type}"

	storage_image_reference {
		publisher 			= "Canonical"
		offer 				= "UbuntuServer"
		sku 				= "14.04.2-LTS"
		version 			= "latest"
	}

	storage_os_disk {
		name 				= "azure-${var.prefix}-web-${format("%02d", count.index + 1)}-osdisk1"
		vhd_uri 			= "https://${var.storage_acct_name}.blob.core.windows.net/${azurerm_storage_container.primary.name}/azure-${var.prefix}-web-${format("%02d", count.index + 1)}-myosdisk1.vhd"
		caching 			= "ReadWrite"
		create_option 		= "FromImage"
	}

	os_profile {
		computer_name 		= "azure-${var.prefix}-web-${format("%02d", count.index + 1)}"
		admin_username 		= "ubuntu"
		admin_password		= "${var.initial_admin_password}"
	}

	os_profile_linux_config {
		disable_password_authentication = true
		ssh_keys {
	      path 				= "/home/ubuntu/.ssh/authorized_keys"
	      key_data 			= "${var.ssh_key}"
	    }
	}

	tags {
		role 				= "${var.role}"
		public_ip			= "${element(azurerm_public_ip.primary.*.ip_address, count.index)}"
		private_ip			= "${element(azurerm_network_interface.primary.*.private_ip_address, count.index)}"
	}
}
