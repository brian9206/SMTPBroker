#!/bin/bash

if [ "$ASPNETCORE_Kestrel__Certificates__Default__Path" == "/ssl/server.pfx" ] && [ ! -f "/ssl/server.pfx" ]; then
    echo "Generating self-signed certificate."
    
    mkdir -p /ssl
    
    openssl rand -base64 48 > /ssl/passphrase.txt
    openssl genrsa -aes128 -passout file:/ssl/passphrase.txt -out /ssl/server.key 2048
    openssl req -new -passin file:/ssl/passphrase.txt -key /ssl/server.key -out /ssl/server.csr -subj "/C=HK/O=localhost/OU=localhost/CN=localhost"
   
    cp /ssl/server.key /ssl/server.key.org
    openssl rsa -in /ssl/server.key.org -passin file:/ssl/passphrase.txt -out /ssl/server.key
    openssl x509 -req -days 3650 -in /ssl/server.csr -signkey /ssl/server.key -out /ssl/server.crt
    
    openssl pkcs12 -export -out /ssl/server.pfx -inkey /ssl/server.key -in /ssl/server.crt -passout pass:
fi

dotnet SMTPBroker.dll $1