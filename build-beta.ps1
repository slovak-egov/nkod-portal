
npm --prefix .\src\frontend\ run build

docker build -f .\src\DocumentStorageApi\Dockerfile -t nkod-documentstorage:beta src
docker tag nkod-documentstorage:beta 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-documentstorage:beta
docker push 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-documentstorage:beta

docker build -f .\src\CodelistProvider\Dockerfile -t nkod-codelistprovider:beta src
docker tag nkod-codelistprovider:beta 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-codelistprovider:beta
docker push 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-codelistprovider:beta

docker build -f .\src\WebApi\Dockerfile -t nkod-webapi:beta src
docker tag nkod-webapi:beta 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-webapi:beta
docker push 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-webapi:beta

docker build -f .\src\IAM\Dockerfile -t nkod-iam:beta src
docker tag nkod-iam:beta 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-iam:beta
docker push 082286734144.dkr.ecr.eu-west-1.amazonaws.com/nkod-iam:beta