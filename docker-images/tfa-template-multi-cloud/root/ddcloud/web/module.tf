/*
 * Web server (Dimension Data)
 * ===========================
 *
 * A web server (and associated resources) in Dimension Data's Managed Cloud Platform (MCP).
 */

##################################
# Dimension Data provider settings

variable "cloudcontrol_user"		{ }
variable "cloudcontrol_password"	{ }
variable "cloudcontrol_region"  	{ }
variable "cloudcontrol_datacenter"	{ }
variable "initial_admin_password"	{ }

provider "ddcloud" {
	username	= "${var.cloudcontrol_user}"
	password 	= "${var.cloudcontrol_password}"

	region		= "${var.cloudcontrol_region}"
}

##################
# General settings

# Prefix for resource names.
variable "prefix"           { }

###################
# Instance settings

# The SSH public key used for authentication to MCP server instances.
variable "ssh_key"          { }

# The role tag for the server.
variable "role"             { default = "web" }

# The number of Server instances to create.
variable "instance_count"   { }

# The public IP from which the deployment is running.
variable "deployment_ip"	{ }

#########
# Outputs

output "hosts" {
	value = ["${ddcloud_server.web.*.name}"]
}
output "public_ips" {
	value = ["${ddcloud_nat.web.*.public_ipv4}"]
}
output "private_ips" {
	value = ["${ddcloud_nat.web.*.private_ipv4}"]
}
