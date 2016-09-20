#!/usr/bin/env bash

docker build -t ddresearch/terraform-ansible-deploy -t ddresearch/terraform-ansible-deploy:stable docker-images/terraform-ansible-deploy
docker build -t ddresearch/tfa-template-multi-cloud -t ddresearch/tfa-template-multi-cloud:stable docker-images/tfa-template-multi-cloud
