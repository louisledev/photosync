# Quick Start: Personal Microsoft Accounts

This guide helps you quickly set up PhotoSync for **personal Microsoft accounts** (outlook.com, hotmail.com, live.com).

## Why This Guide?

Personal Microsoft accounts **cannot be managed** through your Azure organization's Entra portal. You need a different authentication approach using **refresh tokens**.

## 5-Minute Setup

### 1. Register ONE Azure AD App (5 min)

1. Go to [Azure Portal](https://portal.azure.com) → **Azure Entra** → **App registrations**
2. Click **New registration**
   - Name: `PhotoSync-MultiAccount`
   - Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**
   - Redirect URI: Web → `http://localhost:8080/callback`
   - Click **Register**

3. Copy the **Client ID** and **Tenant ID** (use `common` for multi-tenant)

4. Create a **Client Secret**:
   - Go to **Certificates & secrets** → **New client secret**
   - Copy the secret value immediately

5. Configure **Delegated Permissions** (NOT Application):
   - Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**
   - Add: `Files.Read`, `Files.ReadWrite`, `offline_access`
   - **DO NOT** click "Grant admin consent" - users consent individually

### 2. Get Refresh Tokens (2 min per account)

```bash
cd tools
node get-refresh-token.js YOUR_CLIENT_ID YOUR_CLIENT_SECRET
```

- Browser opens → Sign in with the account to authorize
- Copy the refresh token displayed in terminal
- Repeat for each account (yours, wife's, shared destination)

### 3. Deploy Infrastructure with Key Vault (5 min)

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars`:

```hcl
# Enable Key Vault for refresh tokens
enable_keyvault = true
key_vault_name  = "photosync-kv-UNIQUE"  # Must be globally unique

# OneDrive configuration (using ONE app registration for all accounts)
onedrive1_config = {
  "OneDrive1:ClientId"        = "your-client-id"
  "OneDrive1:TenantId"        = "common"
  "OneDrive1:ClientSecret"    = "source1-refresh-token"  # Key Vault secret name
  "OneDrive1:SourceFolder"    = "Pictures/CameraRoll"
  "OneDrive1:DeleteAfterSync" = "false"
}

onedrive2_config = {
  "OneDrive2:ClientId"        = "your-client-id"  # Same as above
  "OneDrive2:TenantId"        = "common"
  "OneDrive2:ClientSecret"    = "source2-refresh-token"  # Key Vault secret name
  "OneDrive2:SourceFolder"    = "Pictures/CameraRoll"
  "OneDrive2:DeleteAfterSync" = "false"
}

onedrive_destination_config = {
  "OneDriveDestination:ClientId"     = "your-client-id"  # Same as above
  "OneDriveDestination:TenantId"     = "common"
  "OneDriveDestination:ClientSecret" = "destination-refresh-token"  # Key Vault secret name
  "OneDriveDestination:DestinationFolder" = "Pictures/FamilyPhotos"
}

# Store refresh tokens (from step 2)
source1_refresh_token      = "0.AXEA..."  # Your refresh token
source2_refresh_token      = "0.AXEA..."  # Wife's refresh token
destination_refresh_token  = "0.AXEA..."  # Shared account refresh token

# Store the actual client secret
source1_client_secret_for_vault      = "your-actual-client-secret"
source2_client_secret_for_vault      = "your-actual-client-secret"  # Same
destination_client_secret_for_vault  = "your-actual-client-secret"  # Same
```

Deploy:

```bash
terraform init
terraform apply
```

### 4. Configure Function Apps for Refresh Token Mode (2 min)

After Terraform completes, configure both Function Apps:

```bash
# Get Function App names
SOURCE1=$(terraform output -raw function_app_source1_name)
SOURCE2=$(terraform output -raw function_app_source2_name)
VAULT_URI=$(terraform output -raw key_vault_uri)

# Configure Function App 1 for refresh token auth
az functionapp config appsettings set \
  --name $SOURCE1 \
  --resource-group photosync-rg \
  --settings \
    "UseRefreshTokenAuth=true" \
    "KeyVault:VaultUrl=$VAULT_URI"

# Configure Function App 2 for refresh token auth
az functionapp config appsettings set \
  --name $SOURCE2 \
  --resource-group photosync-rg \
  --settings \
    "UseRefreshTokenAuth=true" \
    "KeyVault:VaultUrl=$VAULT_URI"
```

### 5. Deploy Function Code (2 min)

```bash
cd ../src
func azure functionapp publish $SOURCE1
func azure functionapp publish $SOURCE2
```

## Done! 🎉

Your PhotoSync is now running with personal Microsoft accounts.

## Testing

Test the configuration:

```bash
# Check Function App logs
az functionapp log tail --name $SOURCE1 --resource-group photosync-rg
```

Or manually trigger:

```bash
az functionapp function invoke \
  --name $SOURCE1 \
  --resource-group photosync-rg \
  --function-name PhotoSyncTimer
```

## How It Works

1. **One App Registration**: All accounts use the same Azure AD app
2. **Delegated Permissions**: Each user consents individually
3. **Refresh Tokens**: Long-lived tokens stored in Key Vault (auto-renewed)
4. **Function Apps**: Use refresh tokens to get access tokens on demand

## Key Points

✅ **One app registration** serves all accounts
✅ **No admin consent** required
✅ **Refresh tokens** last ~90 days and auto-renew
✅ **Secure storage** in Azure Key Vault
✅ **Managed identities** for Function Apps to access Key Vault
✅ **Users can revoke** access anytime via Microsoft account settings

## Troubleshooting

**"Failed to retrieve refresh token from Key Vault"**
- Ensure Function App managed identity has Key Vault access (Terraform handles this)
- Check Key Vault access policies in Azure Portal

**"Token exchange failed"**
- Verify the refresh token is still valid
- Re-run `get-refresh-token.js` to get a new token
- Update the secret in Key Vault

**"Authentication failed"**
- Check that `UseRefreshTokenAuth=true` is set
- Verify `KeyVault:VaultUrl` is correct
- Ensure the client secret is also stored in Key Vault (or in config)

## Next Steps

- See [PERSONAL_ACCOUNTS_SETUP.md](PERSONAL_ACCOUNTS_SETUP.md) for detailed documentation
- Monitor logs in Azure Portal
- Set up Application Insights for better monitoring

---

**Cost**: Same as before (~$2.50-3/month) + ~$0.10/month for Key Vault
