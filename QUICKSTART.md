# Quick Start Guide - Photo Sync (Personal Accounts)

This guide will get you up and running in under 30 minutes with **personal Microsoft accounts** (outlook.com, hotmail.com, live.com).

## Prerequisites Checklist

- [ ] Azure subscription ([Get free trial](https://azure.microsoft.com/free/))
- [ ] .NET 8.0 SDK installed
- [ ] Azure Functions Core Tools installed
- [ ] Terraform installed
- [ ] Azure CLI installed
- [ ] Node.js installed (for refresh token script)
- [ ] Access to the personal Microsoft accounts you want to sync

## Step-by-Step Setup

### 1. Register ONE App in Azure AD (5 minutes)

You only need **one** app registration for all accounts:

1. Go to https://portal.azure.com
2. Navigate to: **Azure Entra** → **App registrations** → **New registration**
3. Fill in:
   - Name: `PhotoSync-MultiAccount`
   - Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**
   - Redirect URI: Web → `http://localhost:8080/callback`
   - Click **Register**

4. **Copy these values** (you'll need them later):
   - Application (client) ID: `abc123...`
   - For Tenant ID, use: `common`

5. Create a client secret:
   - Go to **Certificates & secrets**
   - Click **New client secret**
   - Description: `PhotoSync`
   - Expires: 24 months
   - Click **Add**
   - **⚠️ COPY THE VALUE NOW** (you can't see it again!)

6. Grant **Delegated** permissions:
   - Go to **API permissions**
   - Click **Add a permission** → **Microsoft Graph** → **Delegated permissions**
   - Search and add:
     - `Files.Read`
     - `Files.ReadWrite`
     - `offline_access`
   - **DO NOT** click "Grant admin consent" - users consent individually

That's it! One app registration for all accounts.

### 2. Get Refresh Tokens (10 minutes)

Run the provided script to get refresh tokens for each personal account:

```bash
cd tools
node get-refresh-token.js YOUR_CLIENT_ID YOUR_CLIENT_SECRET
```

**What happens:**
1. Browser opens automatically
2. Sign in with the first Microsoft account
3. Grant permissions when prompted
4. Copy the refresh token from terminal output
5. Save it securely

**Repeat for all 3 accounts:**
- Your personal account → save as `source1_refresh_token`
- Wife's personal account → save as `source2_refresh_token`
- Shared destination account → save as `destination_refresh_token`

**Important:** These tokens are long-lived (~90 days) and auto-renew. Store them securely!

### 3. Configure Terraform (5 minutes)

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your values:

```hcl
# Enable Key Vault for secure token storage
enable_keyvault = true
key_vault_name  = "photosync-kv-UNIQUE"  # Change UNIQUE to something random

# Same client ID for all accounts
# Note: Tenant is always "common" for personal accounts (hardcoded in auth provider)
onedrive1_config = {
  "OneDrive1:ClientId"               = "your-client-id"
  "OneDrive1:RefreshTokenSecretName" = "source1-refresh-token"
  "OneDrive1:ClientSecretName"       = "source1-client-secret"
  "OneDrive1:SourceFolder"           = "/Photos"
  "OneDrive1:DeleteAfterSync"        = "false"
  "OneDrive1:MaxFilesPerRun"         = "100"  # Limit files per run to prevent timeout
}

onedrive2_config = {
  "OneDrive2:ClientId"               = "your-client-id"  # Same as above
  "OneDrive2:RefreshTokenSecretName" = "source2-refresh-token"
  "OneDrive2:ClientSecretName"       = "source2-client-secret"
  "OneDrive2:SourceFolder"           = "/Pictures"
  "OneDrive2:DeleteAfterSync"        = "false"
  "OneDrive2:MaxFilesPerRun"         = "100"  # Limit files per run to prevent timeout
}

onedrive_destination_config = {
  "OneDriveDestination:ClientId"               = "your-client-id"  # Same as above
  "OneDriveDestination:RefreshTokenSecretName" = "destination-refresh-token"
  "OneDriveDestination:ClientSecretName"       = "destination-client-secret"
  "OneDriveDestination:DestinationFolder"      = "/Synced Photos"
}

# Paste the refresh tokens from Step 2
source1_refresh_token      = "0.AXEA..."  # Your token
source2_refresh_token      = "0.AXEA..."  # Wife's token
destination_refresh_token  = "0.AXEA..."  # Shared token

# Paste the client secret from Step 1 (same for all)
source1_client_secret_for_vault      = "your-client-secret"
source2_client_secret_for_vault      = "your-client-secret"
destination_client_secret_for_vault  = "your-client-secret"
```

### 4. Deploy Infrastructure (5 minutes)

```bash
# Login to Azure
az login

# Deploy with Terraform
terraform init
terraform plan
terraform apply
```

Type `yes` when prompted. This creates:
- Two Function Apps (one for each source account)
- Azure Key Vault (for refresh tokens) with automatic configuration
- Storage accounts (for state tracking)
- Application Insights (for monitoring)
- All necessary app settings including Key Vault URL

### 5. Deploy Function Code (2 minutes)

```bash
# Get the deployment outputs
SOURCE1=$(terraform output -raw function_app_source1_name)
SOURCE2=$(terraform output -raw function_app_source2_name)

# Deploy to both Function Apps
cd ../src
func azure functionapp publish $SOURCE1
func azure functionapp publish $SOURCE2
```

## Done! 🎉

Your PhotoSync is now running with personal Microsoft accounts using secure refresh token authentication.

## Verification

### Check Logs

View logs in Application Insights:

```bash
# Get the Application Insights logs URL
cd terraform
terraform output logs_portal_url

# Or view logs directly in Azure Portal
# Navigate to: Application Insights → photosync-insights → Logs
```

You can also query logs using KQL (Kusto Query Language):
```kql
traces
| where timestamp > ago(1h)
| where cloud_RoleName contains "photosync"
| order by timestamp desc
| project timestamp, message, severityLevel
```

### Manually Trigger (Optional)

To test immediately without waiting for the schedule, use the trigger script:

```bash
# Trigger both Function Apps
./trigger-sync.sh

# Trigger only Source 1
./trigger-sync.sh --source1-only

# Trigger and view logs in Application Insights
./trigger-sync.sh --logs
```

### Verify Photos

1. Check your destination OneDrive account (sign in at onedrive.com)
2. Navigate to the destination folder (e.g., `/Synced Photos`)
3. Look for photos organized by date: `2025/2025-12/20231225_143022.jpg`

## Schedule

The function runs automatically every hour. To change:
1. Open `PhotoSyncFunction.cs`
2. Change the cron expression: `[TimerTrigger("0 0 * * * *")]`

Examples:
- Every 6 hours: `"0 0 */6 * * *"`
- Every day at 8 PM: `"0 0 20 * * *"`
- Twice daily (6 AM & 6 PM): `"0 0 6,18 * * *"`

## Configuration Options

### MaxFilesPerRun

The `MaxFilesPerRun` setting limits how many files are processed in a single run. This is useful for:
- **Initial sync**: When you have many files, process them incrementally to avoid timeout
- **Consumption plan limits**: Azure Functions Consumption plan has a 10-minute timeout
- **Controlled syncing**: Process files in smaller batches for better monitoring

**Recommended values:**
- **100-200 files**: Good balance for initial sync with many files
- **Unlimited**: Set to a very high number (e.g., `"999999"`) once initial sync is complete
- **Adjust based on file sizes**: Larger photos/videos may need lower limits

Example: If you have 1000 photos and set `MaxFilesPerRun = "100"`, it will take 10 runs (10 days at daily schedule) to sync all files.

## Troubleshooting

### "Failed to retrieve refresh token from Key Vault"
- Ensure Function App managed identity has Key Vault access (Terraform handles this automatically)
- Check Key Vault access policies in Azure Portal
- Verify `KeyVault:VaultUrl` is set correctly

### "Token exchange failed"
- The refresh token may have expired or been revoked
- Re-run `tools/get-refresh-token.js` to get a new token
- Update the secret in Key Vault

### "Authentication failed"
- Verify the client secret is correct in Key Vault
- Make sure you used `common` as the tenant ID
- Check that refresh tokens are stored correctly in Key Vault

### No photos syncing
- Check Function App logs for specific errors
- Verify source folders contain photos
- Ensure folder paths are correct (relative to OneDrive root)
- Check that photos have supported extensions (.jpg, .png, .heic, etc.)

## Cost

For typical use (500 photos/month with 2 Function Apps):
- Azure Functions (2 apps): ~$0.40/month
- Storage (2 accounts): ~$0.10/month
- Key Vault: ~$0.10/month
- Data transfer: ~$1-2/month

**Total: ~$2.50-3/month**

## How It Works

1. **One App Registration**: All accounts use the same Azure AD app
2. **Delegated Permissions**: Each user consents individually when getting their refresh token
3. **Refresh Tokens**: Long-lived tokens (~90 days) stored securely in Azure Key Vault
4. **Auto-Renewal**: Tokens automatically renew when used, so they never expire
5. **Managed Identities**: Function Apps access Key Vault securely without passwords
6. **User Control**: Users can revoke access anytime via their Microsoft account settings

## Next Steps

- Monitor logs for the next few days
- Verify photos are syncing correctly with date-based folders
- See [PERSONAL_ACCOUNTS_SETUP.md](PERSONAL_ACCOUNTS_SETUP.md) for detailed documentation
- Check [README.md](README.md) for customization options

## Support

- Check Function App logs in Azure Portal
- Review [PERSONAL_ACCOUNTS_SETUP.md](PERSONAL_ACCOUNTS_SETUP.md) for detailed troubleshooting
- See [terraform/TERRAFORM.md](terraform/TERRAFORM.md) for infrastructure documentation
