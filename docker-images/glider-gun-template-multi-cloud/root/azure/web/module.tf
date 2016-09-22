/*
 * Web server (Azure)
 * ==================
 *
 * A web server (and associated resources) in Microsoft Azure.
 */

#########################
# Azure provider settings

variable "subscription_id"			{ }
variable "client_id"   				{ }
variable "client_secret"			{ }
variable "tenant_id"       			{ }
variable "region"  					{ }
variable "region_name"  			{ }
variable "storage_rg_name"			{ }
variable "storage_acct_name"		{ }
variable "initial_admin_password"	{ }

provider "azurerm" {
	subscription_id = "${var.subscription_id}"
	client_id       = "${var.client_id}"
	client_secret   = "${var.client_secret}"
	tenant_id       = "${var.tenant_id}"
}

##################
# General settings

# Prefix for resource names.
variable "prefix"           { }

###################
# Instance settings

# The number of Azure VM instances to create.
variable "instance_count"   { }

# The type of Azure VM instances to create.
variable "instance_type"   { }

# The SSH public key used for authentication to Azure VM instances.
variable "ssh_key"          { }

# The role tag for the server.
variable "role"             { default = "web" }

# The IP address from which to permit deployment traffic.
variable "deployment_ip"    { }

#########
# Outputs

output "hosts" {
	value = ["${azurerm_virtual_machine.web.*.name}"]
}
output "public_ips" {
	value = ["${azurerm_public_ip.primary.*.ip_address}"]
}
output "private_ips" {
	value = ["${azurerm_network_interface.primary.*.private_ip_address}"]
}
