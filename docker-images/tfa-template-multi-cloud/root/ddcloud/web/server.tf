variable "web_network_address_start" {
	default = 20
}

resource "ddcloud_server" "web" {
	count 					= "${var.instance_count}"

	name 					= "ddcloud-${var.prefix}-web-${format("%02d", count.index + 1)}"
	description 			= "Web server for ${var.prefix} multi-cloud deployment demo."
	
	auto_start				= true
	admin_password			= "${var.initial_admin_password}"

	memory_gb				= 8

	networkdomain 			= "${ddcloud_networkdomain.primary.id}"
	primary_adapter_vlan	= "${ddcloud_vlan.primary.id}"
	# Address must be pre-configured because we're (potentially) deploying multiple servers simultaneously on the same VLAN.
	primary_adapter_ipv4 	= "${var.network_prefix}.${var.web_network_address_start + count.index}"
	dns_primary				= "8.8.8.8"
	dns_secondary			= "8.8.4.4"

	os_image_name			= "Ubuntu 14.04 2 CPU"
	
	disk {
		scsi_unit_id		= 0
		size_gb				= 10
	}

	tag {
		name = "role"
		value = "${var.role}"
	}
}
