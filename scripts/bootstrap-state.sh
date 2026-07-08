#!/usr/bin/env bash
# One-time setup of the Terraform remote-state storage account.
# Requires: az login already done. Safe to re-run (idempotent).
set -euo pipefail

LOCATION="${LOCATION:-eastus}"
STATE_RG="greppa-tfstate-rg"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
# Deterministic, globally-unique-enough name derived from the subscription.
STORAGE_ACCOUNT="greppatf$(echo "$SUBSCRIPTION_ID" | tr -d '-' | cut -c1-14)"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "==> Creating state resource group '$STATE_RG' in $LOCATION"
az group create --name "$STATE_RG" --location "$LOCATION" --output none

echo "==> Creating storage account '$STORAGE_ACCOUNT' (private, TLS 1.2, LRS)"
az storage account create \
  --name "$STORAGE_ACCOUNT" \
  --resource-group "$STATE_RG" \
  --location "$LOCATION" \
  --sku Standard_LRS \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false \
  --output none

echo "==> Creating blob container 'tfstate'"
az storage container create \
  --name tfstate \
  --account-name "$STORAGE_ACCOUNT" \
  --auth-mode login \
  --output none

echo "==> Writing terraform/backend.tf"
cat > "$REPO_ROOT/terraform/backend.tf" <<EOF
# Remote state storage. Created once by scripts/bootstrap-state.sh, which rewrites
# this file with the generated storage account name.
terraform {
  backend "azurerm" {
    resource_group_name  = "$STATE_RG"
    storage_account_name = "$STORAGE_ACCOUNT"
    container_name       = "tfstate"
    key                  = "greppa.tfstate"
  }
}
EOF

echo
echo "Done. Remote state: $STORAGE_ACCOUNT/tfstate/greppa.tfstate"
echo "Commit the updated terraform/backend.tf."
