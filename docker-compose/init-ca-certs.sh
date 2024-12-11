#!/usr/bin/env bash
set -euo pipefail

# Variables
TAR_URL="https://example.com/ca-certificates.tar.gz" # Update to actual URL
DOWNLOAD_DIR="/tmp"
CERTS_DIR="$DOWNLOAD_DIR/ca-certs"

# 1) Install dotnet-certs tool globally
dotnet tool install --global dotnet-certs || {
    echo "Failed to install dotnet-certs"
    exit 1
}

# Ensure the .NET global tools directory is on the PATH
export PATH="$PATH:$HOME/.dotnet/tools"

# 2) Download the tar.gz file containing CA root certificates
curl -fSL "$TAR_URL" -o "$DOWNLOAD_DIR/ca-certificates.tar.gz"

# 3) Extract the downloaded tar.gz into a temporary directory
mkdir -p "$CERTS_DIR"
tar -xzf "$DOWNLOAD_DIR/ca-certificates.tar.gz" -C "$CERTS_DIR"

# 4) Iterate through each certificate and use dotnet-certs to inject it into the .NET "My" store
for cert in "$CERTS_DIR"/*.crt; do
    if [[ -f "$cert" ]]; then
        echo "Importing $cert into the .NET 'My' store..."
        dotnet-certs add --store My --cert "$cert" || {
            echo "Failed to import $cert"
            exit 1
        }
    fi
done

echo "All certificates have been successfully imported."