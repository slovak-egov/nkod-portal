nerdctl build --namespace=k8s.io -f ../src/IAM/Dockerfile -t nkod/iam:snapshot ../src
nerdctl build --namespace=k8s.io -f ../src/CodelistProvider/Dockerfile -t nkod/codelistprovider:snapshot ../src
nerdctl build --namespace=k8s.io -f ../src/DocumentStorageApi/Dockerfile -t nkod/documentstorage:snapshot ../src
nerdctl build --namespace=k8s.io -f ../src/WebApi/Dockerfile -t nkod/webapi:snapshot ../src
nerdctl build --namespace=k8s.io -f ../src/CMS/Dockerfile -t nkod/cms:snapshot ../src
