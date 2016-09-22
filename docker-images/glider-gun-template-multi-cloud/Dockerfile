# Templated multi-cloud deployment: AWS, Azure, and Dimension Data Cloud Control.

FROM ddresearch/glider-gun-template-base-tfa:stable

MAINTAINER Adam Friedman <adam.friedman@itaas.dimensiondata.com>

COPY root /root

# Ensure permissions are right for ssh key auth.
RUN chmod 700 ~/.ssh && chmod 600 ~/.ssh/*

# Link / fetch any sub-modules, as required.
RUN ["terraform", "get", "-update=true"]

# Image metadata
LABEL com.dimensiondata.image-type="glider-gun.template" \
      com.dimensiondata.glider-gun.template.supported-actions="deploy,destroy"
