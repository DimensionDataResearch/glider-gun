---
layout: page
title:  "Design"
date:   2016-09-21 05:21:00
categories: design
---

# High-level design

Glider Gun uses Docker to encapsulate the process for each deployment, providing additional configuration (e.g. terraform variables) via a mounted volume, and then capturing the resulting outputs (e.g. terraform state, log files) via that same mounted volume.

## Templates

### Templates as Docker images

![templates as docker images](../images/templates-as-docker-images.jpg)

## Deploying a template

![deploying a template](../images/deploying-a-template.jpg)
