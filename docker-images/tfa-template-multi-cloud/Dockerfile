# Templated multi-cloud deployment: AWS, Azure, and Dimension Data Cloud Control.

FROM ddresearch/terraform-ansible-deploy:stable

COPY root /root

# Ensure permissions are right for ssh key auth.
RUN chmod 700 ~/.ssh && chmod 600 ~/.ssh/*

# Link up any sub-modules, as required.
RUN ["terraform", "get", "-update=true"]
