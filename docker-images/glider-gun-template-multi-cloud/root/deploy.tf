variable "app_name"                 { }
variable "ssh_key"                  { default = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDnqvOY7lS+gNrnbTLgaKKRLbLjb8C3VTZ4AJ3vlGQ1fFMMnqWsKAT+dQ0PnnCLLOgfeYyGkH7Hh8pwPi5x+euQdBscy9N3iifvgSzrmkOaTnTmzL3y1KM8ZNmvJJ4O9HVWFgcD9bU85jFGBHZw6T0rxCPnUslg1y0Bu9GlXKITAhLsO6slzgt+jn2qJ7ddQC4dni4z1F9mzEQgruCpO2dBaVXFLsrbc5Vh2sGZKu04uIq0roq5xvLWLTTNuTaAm83Zqo9UPIc7dvI5DrG1LHVdoVEG9QM9pieV1xiuo1bLnNuYcGpdWh1aTYEFY+sW1loJsXeUEEmfFA2Oanx05K2L tintoy@skinny-latte.local" }

# Initial password used to bootstrap SSH keys.
variable "initial_admin_password"   { }

# The public IP address of the host running the deployment (used to permit SSH access through firewalls).
variable "deployment_ip"        { }

################
# Dimension Data

variable "ddcloud_instance_count"           { }
variable "ddcloud_user"                     { sensitive = true }
variable "ddcloud_password"                 { sensitive = true }
variable "ddcloud_region"                   { }
variable "ddcloud_datacenter"               { }

module "ddcloud" {
    source                  = "./ddcloud/web"

    prefix                  = "${var.app_name}"

    instance_count          = "${var.ddcloud_instance_count}"
    
    ssh_key                 = "${var.ssh_key}"
    deployment_ip           = "${var.deployment_ip}"

    cloudcontrol_user       = "${var.ddcloud_user}"
    cloudcontrol_password   = "${var.ddcloud_password}"
    cloudcontrol_region     = "${var.ddcloud_region}"
    cloudcontrol_datacenter = "${var.ddcloud_datacenter}"

    initial_admin_password  = "${var.initial_admin_password}"
}

output "ddcloud_hosts" {
    value                   = [ "${module.ddcloud.hosts}" ]
}
output "ddcloud_public_ips" {
    value                   = [ "${module.ddcloud.public_ips}" ]
}
output "ddcloud_private_ips" {
    value                   = [ "${module.ddcloud.private_ips}" ]
}

#####
# AWS
variable "aws_instance_type"    { default = "t1.micro" }
variable "aws_instance_count"   { }
variable "aws_access_key"       { sensitive = true }
variable "aws_secret_key"       { sensitive = true }
variable "aws_region"           { }

module "aws" {
    source          = "./aws/web"

    prefix          = "${var.app_name}"
    instance_type   = "${var.aws_instance_type}"
    instance_count  = "${var.aws_instance_count}"
    
    ssh_key         = "${var.ssh_key}"
    deployment_ip   = "${var.deployment_ip}"

    access_key      = "${var.aws_access_key}"
    secret_key      = "${var.aws_secret_key}"
    region          = "${var.aws_region}"
}

output "aws_hosts" {
    value       = [ "${module.aws.hosts}" ]
}
output "aws_public_ips" {
    value       = [ "${module.aws.public_ips}" ]
}
output "aws_private_ips" {
    value       = [ "${module.aws.private_ips}" ]
}

#######
# Azure

variable "azure_instance_type"      { default = "Standard_A0" }
variable "azure_instance_count"     { }
variable "azure_subscription_id"    { sensitive = true }
variable "azure_client_id"          { sensitive = true }
variable "azure_client_secret"      { sensitive = true }
variable "azure_tenant_id"          { sensitive = true }
variable "azure_region"             { }
variable "azure_region_name"        { }
variable "azure_storage_rg_name"    { }
variable "azure_storage_acct_name"  { }

module "azure" {
    source                  = "./azure/web"

    prefix                  = "${var.app_name}"

    instance_count          = "${var.azure_instance_count}"
    instance_type           = "${var.azure_instance_type}"
    
    ssh_key                 = "${var.ssh_key}"
    deployment_ip           = "${var.deployment_ip}"

    subscription_id         = "${var.azure_subscription_id}"
    client_id               = "${var.azure_client_id}"
    client_secret           = "${var.azure_client_secret}"
    tenant_id               = "${var.azure_tenant_id}"
    region                  = "${var.azure_region}"
    region_name             = "${var.azure_region_name}"
    storage_rg_name         = "${var.azure_storage_rg_name}"
    storage_acct_name       = "${var.azure_storage_acct_name}"

    initial_admin_password  = "${var.initial_admin_password}"
}

output "azure_hosts" {
    value                   = [ "${module.azure.hosts}" ]
}
output "azure_public_ips" {
    value                   = [ "${module.azure.public_ips}" ]
}
output "azure_private_ips" {
    value                   = [ "${module.azure.private_ips}" ]
}
