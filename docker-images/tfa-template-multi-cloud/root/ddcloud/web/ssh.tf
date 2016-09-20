resource "null_resource" "ssh_key" {
	count 			= "${var.instance_count}"

	# Install our SSH public key (will be used by Ansible).
	provisioner "remote-exec" {
		inline = [
			"mkdir -p ~/.ssh",
			"chmod 700 ~/.ssh",
			"echo '${var.ssh_key}' > ~/.ssh/authorized_keys",
			"chmod 600 ~/.ssh/authorized_keys"
		]

		connection {
			type 		= "ssh"
			
			user 		= "root"
			password 	= "${var.initial_admin_password}"

			host 		= "${element(ddcloud_nat.web.*.public_ipv4, count.index)}"
		}
	}

	depends_on 			= [
		"ddcloud_server.web",
		"ddcloud_nat.web",
		"ddcloud_firewall_rule.ssh4-in",
		"ddcloud_firewall_rule.http4-in"
	]
}
