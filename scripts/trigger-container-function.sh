#!/bin/bash

if [ -z "$1" ]; then
    echo "Usage: $0 <function-name>"
    echo "Example: $0 FetchNpwdIssuedPrnsFunction"
    exit 1
fi

FUNCTION_NAME=$1

hostJsonContents=$(docker compose -f function.yml exec prn-functions cat /azure-functions-host/Secrets/host.json)
functionMasterKey=$(echo "$hostJsonContents" | jq -r '.masterKey.value')

curl -v POST http://localhost:5800/admin/functions/$FUNCTION_NAME \
-H "Content-Type: application/json" \
-H "x-functions-key: $functionMasterKey" \
-d '{}'