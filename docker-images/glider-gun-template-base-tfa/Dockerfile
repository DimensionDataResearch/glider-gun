# Base image for Glider Gun templates using Terraform + Ansible.

FROM ddresearch/terraform-provider-ddcloud:v1.0

MAINTAINER Adam Friedman <adam.friedman@itaas.dimensiondata.com>

RUN apk add --update bash python py-pip
RUN pip install --upgrade pip

RUN apk add ansible sshpass

COPY root /root
WORKDIR /root

# Don't prompt for input.
ENV TF_INPUT=0
ENV TF_VARIABLES_FILE=/root/state/tfvars.json

# Maximum degree of parallelism for Terraform
ENV MAX_TF_PARALLELISM=10

RUN mkdir -p /root/state
VOLUME /root/state

# TODO: Change this to /entry-point.sh so that Glider Gun can pass the action (e.g. "deploy", "destroy", etc) as an argument.
ENTRYPOINT ["/bin/bash", "/root/deploy.sh"]
