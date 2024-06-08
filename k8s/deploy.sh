export REPOSITORY=nkod
export TAG=snapshot
export STORAGE_CLASS=local-path
export STORAGE_SIZE=500Mi
export IAMDB_SIZE=500Mi
export ACCESS_KEY_PASS=accesskeypass
export DB_PASS=dbpass
export SAML_CERT_PASS=\!QAZ2wsx
export DB_CONNECTION_STRING=iamdb;Database=iamdb;Uid=root;Pwd=${DB_PASS};


openssl req -nodes -x509 -newkey rsa:2048 -keyout signing.key \
    -out cert.pem -days 3650 -subj "/CN=demo"
openssl pkcs12 -in cert.pem -inkey signing.key \
    -export -out signing.pfx -password pass:$ACCESS_KEY_PASS
openssl rsa -pubout -in signing.key -out public.key

kubectl create namespace nodc

kubectl create secret generic nkod-secrets --namespace=nodc \
  --from-literal=AccessTokenKey="$(base64 -w 0 ./signing.pfx)" \
  --from-literal=SigningCertificateFile="$(base64 -w 0 ./itfoxtec.identity.saml2.testidpcore_Certificate.pfx)" \
  --from-literal=DecryptionCertificateFile="$(base64 -w 0 ./itfoxtec.identity.saml2.testwebappcore_Certificate.pfx)" \
  --from-literal=SigningCertificatePassword="${SAML_CERT_PASS}" \
  --from-literal=DecryptionCertificatePassword="${SAML_CERT_PASS}" \
  --from-literal=AccessTokenKeyPassword="${ACCESS_KEY_PASS}" \
  --from-literal=MysqlPassword="${DB_PASS}" \
  --from-literal=MysqlConnectionString="${DB_CONNECTION_STRING}" \
  --from-file=JwtPublicKey=./public.key

mkdir -p out
for f in *.yaml; do envsubst < "$f" > "out/$f"; done
kubectl apply -f out/