#!/usr/bin/env bash
# One-time setup of GitHub Actions -> Azure OIDC federated identity.
# Usage: ./scripts/bootstrap-oidc.sh <github-owner>/<repo>
# Requires: az login already done. Safe to re-run (idempotent).
set -euo pipefail

REPO="${1:?usage: $0 <github-owner>/<repo>}"
APP_NAME="greppa-github-deploy"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

echo "==> Creating app registration '$APP_NAME'"
APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv)
if [ -z "$APP_ID" ]; then
  APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
fi

echo "==> Ensuring service principal exists"
az ad sp show --id "$APP_ID" --output none 2>/dev/null || az ad sp create --id "$APP_ID" --output none

echo "==> Creating federated credential for $REPO (main branch)"
az ad app federated-credential create --id "$APP_ID" --parameters "{
  \"name\": \"greppa-main\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$REPO:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}" --output none 2>/dev/null || echo "    (federated credential already exists)"

echo "==> Assigning Contributor on the subscription"
az role assignment create \
  --assignee "$APP_ID" \
  --role Contributor \
  --scope "/subscriptions/$SUBSCRIPTION_ID" \
  --output none 2>/dev/null || echo "    (role assignment already exists)"

echo "==> Assigning Storage Blob Data Contributor for Terraform state access"
az role assignment create \
  --assignee "$APP_ID" \
  --role "Storage Blob Data Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/greppa-tfstate-rg" \
  --output none 2>/dev/null || echo "    (role assignment already exists)"

echo
echo "Add these GitHub Actions secrets (Settings -> Secrets and variables -> Actions):"
echo "  AZURE_CLIENT_ID       = $APP_ID"
echo "  AZURE_TENANT_ID       = $TENANT_ID"
echo "  AZURE_SUBSCRIPTION_ID = $SUBSCRIPTION_ID"
echo "  OPENAI_API_KEY        = <from your .env>"
echo "  SEMGREP_APP_TOKEN     = <from your .env>"
echo
if command -v gh >/dev/null 2>&1; then
  echo "Or run:"
  echo "  gh secret set AZURE_CLIENT_ID --repo $REPO --body $APP_ID"
  echo "  gh secret set AZURE_TENANT_ID --repo $REPO --body $TENANT_ID"
  echo "  gh secret set AZURE_SUBSCRIPTION_ID --repo $REPO --body $SUBSCRIPTION_ID"
  echo "  gh secret set OPENAI_API_KEY --repo $REPO"
  echo "  gh secret set SEMGREP_APP_TOKEN --repo $REPO"
fi
