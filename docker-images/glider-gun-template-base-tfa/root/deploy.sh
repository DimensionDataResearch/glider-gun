#!/bin/bash

set -e

DATA_DIR=/root
STATE_DIR=$DATA_DIR/state
LOG_DIR=$STATE_DIR/logs
if [ ! -d $LOG_DIR ]; then
    mkdir -p $LOG_DIR
fi

TF_STATE_FILE=$STATE_DIR/terraform.tfstate

echo "State directory ('$STATE_DIR'):"
ls -lR $STATE_DIR

if [ ! -f "$DATA_DIR/deploy.tf" ]; then
    echo "'$DATA_DIR/deploy.tf' is not present; this image cannot be run directly; create an image based on it that includes a Terraform configuration file called deploy.tf."

    exit 1
fi
if [ ! -z $TF_VARIABLES_FILE ]; then
    if [ ! -f $TF_VARIABLES_FILE ]; then
        echo "Terraform variable override '$TF_VARIABLES_FILE' specified, but not found."

        exit 1
    fi

    echo "Terraform variable override '$TF_VARIABLES_FILE' detected; will use variables from this file."
    TF_VARIABLE_OVERRIDE="-var-file=$TF_VARIABLES_FILE"
fi

if [ ! -f "$DATA_DIR/deploy.yml" ]; then
    echo "'$DATA_DIR/deploy.yml' is not present; this image cannot be run directly; create an image based on it that includes an Ansible playbook called deploy.yml."

    exit 1
fi

# Pre-deploy hook
if [ -f "$DATA_DIR/pre_deploy.sh" ]; then
    echo "Executing pre-deployment hook '$DATA_DIR/pre_deploy.sh' (log = '$LOG_DIR/pre_deploy.log')..."
    bash pre_deploy.sh &> $LOG_DIR/pre_deploy.log
fi

# User infrastructure.
pushd $DATA_DIR

echo "Applying Terraform configuration (log = '$LOG_DIR/terraform-apply.log')..."
terraform apply -no-color -input=false -parallelism=${MAX_TF_PARALLELISM:-10} -state=$TF_STATE_FILE $TF_VARIABLE_OVERRIDE &> "$LOG_DIR/terraform-apply.log"

echo "Dumping Terraform outputs to '$STATE_DIR/terraform.output.json'..."
terraform output -json -state=$TF_STATE_FILE &> $STATE_DIR/terraform.output.json

echo "Refreshing Terraform configuration (log = '$LOG_DIR/terraform-refresh.log')..."
terraform refresh -no-color -input=false -parallelism=${MAX_TF_PARALLELISM:-10} -state=$TF_STATE_FILE $TF_VARIABLE_OVERRIDE &> "$LOG_DIR/terraform-refresh.log"

popd # $DATA_DIR

# Wait for deployed hosts to come up. 
echo "Waiting for hosts to become available (log = '$LOG_DIR/ansible-wait-for-hosts.log')..."
ansible-playbook $DATA_DIR/playbooks/wait-for-hosts.yml &> $LOG_DIR/ansible-wait-for-hosts.log

# User deployment.
echo "Performing Ansible deployment (log = '$LOG_DIR/ansible-deploy.log')..."
ansible-playbook $DATA_DIR/deploy.yml &> $LOG_DIR/ansible-deploy.log

# Post-deploy hook
if [ -f "$DATA_DIR/post_deploy.sh" ]; then
    echo "Executing post-deployment hook '$DATA_DIR/post_deploy.sh' (log = '$LOG_DIR/post_deploy.log')..."
    bash $DATA_DIR/after_deploy.sh &> $LOG_DIR/post_deploy.log
fi

echo "Deployment complete."
