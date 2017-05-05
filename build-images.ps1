docker build -t ddresearch/glider-gun-template-base-tfa -t ddresearch/glider-gun-template-base-tfa:stable .\docker-images\glider-gun-template-base-tfa
docker build -t ddresearch/glider-gun-template-multi-cloud -t ddresearch/glider-gun-template-multi-cloud:stable .\docker-images\glider-gun-template-multi-cloud
docker build -t ddresearch/glider-gun-api -t ddresearch/glider-gun-api:stable -f .\docker-images\glider-gun-api\Dockerfile .
