nerdctl build --namespace=k8s.io -f .\IAM\Dockerfile -t nkod/iam:snapshot .
nerdctl build --namespace=k8s.io -f .\CodelistProvider\Dockerfile -t nkod/codelistprovider:snapshot .
nerdctl build --namespace=k8s.io -f .\DocumentStorageApi\Dockerfile -t nkod/documentstorageapi:snapshoy .
nerdctl build --namespace=k8s.io -f .\WebApi\Dockerfile -t nkod/webapi:snapshot .
