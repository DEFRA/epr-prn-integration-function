#!/bin/bash

if [ -z "$1" ]; then
    echo "Usage: $0 <function-name>"
    echo "Example: $0 FetchNpwdIssuedPrnsFunction"
    exit 1
fi

FUNCTION_NAME=$1

curl -v POST http://localhost:7234/admin/functions/$FUNCTION_NAME -H "Content-Type: application/json" -d '{}'