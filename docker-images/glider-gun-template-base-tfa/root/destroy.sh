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

# User infrastructure.
pushd $DATA_DIR

echo "Destroying Terraform configuration (log = '$LOG_DIR/terraform-destroy.log')..."
terraform destroy -force -no-color -parallelism=${MAX_TF_PARALLELISM:-10} -state=$TF_STATE_FILE $TF_VARIABLE_OVERRIDE &> "$LOG_DIR/terraform-destroy.log"

popd # $DATA_DIR

echo "Destroy complete."
