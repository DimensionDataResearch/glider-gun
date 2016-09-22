
/*
 * Web server (AWS)
 * ================
 *
 * A web server (and associated resources) in Amazon EC2.
 */


#######################
# AWS provider settings

variable "access_key"   { }
variable "secret_key"   { }
variable "region"       { }

provider "aws" {
	access_key = "${var.access_key}"
	secret_key = "${var.secret_key}"
	region = "${var.region}"
}

##################
# General settings

# Prefix for resource names.
variable "prefix"           { }

###################
# Instance settings

# The EC2 instance type to create.
variable "instance_type"    { }

# The number of EC2 instances to create.
variable "instance_count"   { }

# The SSH public key used for authentication to EC2 instances.
variable "ssh_key"          { }

# The role tag for the server.
variable "role"             { default = "web" }

# The IP address from which to permit deployment traffic.
variable "deployment_ip"    { }

##########
# SSH keys

resource "aws_key_pair" "deployment" {
  key_name = "${var.prefix}"
  public_key = "${var.ssh_key}"
}

###############
# Compute (EC2)

# Look up the Amazon Machine Image (AMI) to use.
data "aws_ami" "ubuntu" {
	most_recent     = true

	filter {
		name        = "name"
		values      = ["ubuntu/images/ebs/ubuntu-trusty-14.04-amd64-server-*"]
	}

	filter {
		name        = "virtualization-type"
		values      = ["paravirtual"]
	}

	# Canonical
	owners          = ["099720109477"]
}

resource "aws_instance" "web" {
	count           = "${var.instance_count}"

	ami             = "${data.aws_ami.ubuntu.id}"
	instance_type   = "${var.instance_type}"

	key_name        = "${aws_key_pair.deployment.key_name}"

	security_groups = [
		"default",
		"${aws_security_group.deployment.name}",
		"${aws_security_group.web.name}"
	]

	tags {
		Name        = "aws-${var.prefix}-web-${format("%02d", count.index + 1)}"
		sshUser     = "ubuntu"
		role        = "${var.role}"
	}
}

###############
# Network (EC2)

resource "aws_security_group" "deployment" {
	name = "${replace(var.prefix, "-", "_")}_deployment_inbound"
	description = "Allow inbound deployment traffic"

	ingress {
		from_port   = 0
		to_port     = 22
		protocol    = "tcp"
		cidr_blocks = [ "${var.deployment_ip}/32" ]
	}
}

resource "aws_security_group" "web" {
	name = "${replace(var.prefix, "-", "_")}_web_inbound"
	description = "Allow inbound web traffic"

	ingress {
		from_port   = 0
		to_port     = 80
		protocol    = "tcp"
		cidr_blocks = [ "0.0.0.0/0" ]
	}
}

output "hosts" {
	value = [ "${aws_instance.web.*.tags.Name}" ]
}
output "public_ips" {
	value = [ "${aws_instance.web.*.public_ip}" ]
}
output "private_ips" {
	value = [ "${aws_instance.web.*.private_ip}" ]
}
