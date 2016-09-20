resource "ddcloud_networkdomain" "primary" {
	name		           = "tfa-${var.prefix}"
	description	           = "Network Domain for ${var.prefix} multi-cloud deployment demo."
	datacenter	           = "${var.cloudcontrol_datacenter}"
}

variable "network_prefix" {
	default = "192.168.17"
}

resource "ddcloud_vlan" "primary" {
	name				= "tfa-${var.prefix}-vlan"
	description 		= "Primary VLAN for ${var.prefix} multi-cloud deployment demo."

	networkdomain 		= "${ddcloud_networkdomain.primary.id}"

	ipv4_base_address	= "${var.network_prefix}.0"
	ipv4_prefix_size	= 24 # 255.255.255.0 = 192.168.17.1 -> 192.168.17.254
}

resource "ddcloud_nat" "web" {
	count 					= "${var.instance_count}"

	networkdomain 			= "${ddcloud_networkdomain.primary.id}"
	private_ipv4			= "${element(ddcloud_server.web.*.primary_adapter_ipv4, count.index)}"

	# In this case, public_ipv4 is computed at deploy time.

	depends_on              = ["ddcloud_vlan.primary"]
}

resource "ddcloud_firewall_rule" "http4-in" {
	name 					= "HTTP.Inbound"
	placement				= "first"
	action					= "accept"
	enabled					= true
	
	ip_version				= "ipv4"
	protocol				= "tcp"

	destination_address		= "any"
	destination_port 		= "80"

	networkdomain 			= "${ddcloud_networkdomain.primary.id}"
}

resource "ddcloud_firewall_rule" "ssh4-in" {
	name 					= "SSH4.Inbound"
	placement				= "first"
	action					= "accept"
	enabled					= true
	
	ip_version				= "ipv4"
	protocol				= "tcp"

	source_address 			= "${var.deployment_ip}"

	destination_address		= "any"
	destination_port 		= "22"

	networkdomain 			= "${ddcloud_networkdomain.primary.id}"

	depends_on              = ["ddcloud_firewall_rule.http4-in"]
}