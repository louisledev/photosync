# Quick Start Guide - Photo Sync

This guide will get you up and running in under 30 minutes.

## Prerequisites Checklist

- [ ] Azure subscription ([Get free trial](https://azure.microsoft.com/free/))
- [ ] .NET 8.0 SDK installed
- [ ] Azure Functions Core Tools installed
- [ ] Access to the OneDrive accounts you want to sync

## Step-by-Step Setup

### 1. Register App in Azure AD (15 minutes)

You need to do this **3 times** (once for each OneDrive account):

**For each account:**

1. Go to https://portal.azure.com
2. Navigate to: **Azure Active Directory** → **App registrations** → **New registration**
3. Fill in:
   - Name: `PhotoSync-Account1` (use descriptive names)
   - Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**
   - Click **Register**

4. **Copy these values** (you'll need them later):
   - Application (client) ID: `abc123...`
   - Directory (tenant) ID: `xyz789...`

5. Create a secret:
   - Go to **Certificates & secrets**
   - Click **New client secret**
   - Description: `PhotoSync`
   - Expires: 24 months
   - Click **Add**
   - **⚠️ COPY THE VALUE NOW** (you can't see it again!)

6. Grant permissions:
   - Go to **API permissions**
   - Click **Add a permission** → **Microsoft Graph** → **Application permissions**
   - Search and add:
     - `Files.Read.All`
     - `Files.ReadWrite.All`
   - Click **Grant admin consent for [Your Tenant]** (important!)

**Repeat for all 3 accounts** and save all the IDs and secrets.

### 2. Configure Local Settings (5 minutes)

1. Open `local.settings.json`
2. Replace the placeholder values with your actual credentials:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "OneDrive1:ClientId": "paste-your-client-id-here",
    "OneDrive1:TenantId": "paste-your-tenant-id-here",
    "OneDrive1:ClientSecret": "paste-your-secret-here",
    "OneDrive1:SourceFolder": "Pictures/CameraRoll",
    
    "OneDrive2:ClientId": "paste-your-client-id-here",
    "OneDrive2:TenantId": "paste-your-tenant-id-here",
    "OneDrive2:ClientSecret": "paste-your-secret-here",
    "OneDrive2:SourceFolder": "Pictures/CameraRoll",
    
    "OneDriveDestination:ClientId": "paste-your-client-id-here",
    "OneDriveDestination:TenantId": "paste-your-tenant-id-here",
    "OneDriveDestination:ClientSecret": "paste-your-secret-here",
    "OneDriveDestination:DestinationFolder": "Pictures/FamilyPhotos"
  }
}
```

**Important:**
- Folder paths use `/` slashes (not `\`)
- No leading slash (use `Pictures/Folder` not `/Pictures/Folder`)
- Check the folder names match your actual OneDrive structure

### 3. Test Locally (5 minutes)

1. Install and start Azurite (Azure Storage Emulator):
   ```bash
   npm install -g azurite
   azurite
   ```

2. In a new terminal, restore packages and build:
   ```bash
   dotnet restore
   dotnet build
   ```

3. Run the function:
   ```bash
   func start
   ```

4. In another terminal, trigger it manually:
   ```bash
   # Windows (PowerShell)
   Invoke-WebRequest -Uri http://localhost:7071/api/ManualSync -Method POST
   
   # Mac/Linux
   curl -X POST http://localhost:7071/api/ManualSync
   ```

5. Watch the console output - you should see:
   - Connection to OneDrive accounts
   - Photos being discovered
   - Files being renamed and uploaded

### 4. Deploy to Azure (5 minutes)

**Option A: Using the provided script**

Windows (PowerShell):
```powershell
.\deploy.ps1 -ResourceGroupName "PhotoSyncRG" -FunctionAppName "my-photo-sync"
```

Mac/Linux:
```bash
chmod +x deploy.sh
./deploy.sh -g PhotoSyncRG -n my-photo-sync
```

**Option B: Manual deployment**

```bash
# Login to Azure
az login

# Create resources
az group create --name PhotoSyncRG --location westeurope

az storage account create --name photosyncstorage123 --location westeurope \
  --resource-group PhotoSyncRG --sku Standard_LRS

az functionapp create --resource-group PhotoSyncRG \
  --consumption-plan-location westeurope --runtime dotnet-isolated \
  --functions-version 4 --name my-photo-sync \
  --storage-account photosyncstorage123 --os-type Linux

# Deploy code
func azure functionapp publish my-photo-sync
```

### 5. Configure Azure Settings (3 minutes)

You need to add the same settings from `local.settings.json` to Azure:

**Option A: Azure Portal (easier)**
1. Go to https://portal.azure.com
2. Find your Function App
3. Go to **Configuration** → **Application settings**
4. Click **New application setting** for each setting
5. Click **Save**

**Option B: Azure CLI (faster)**
```bash
az functionapp config appsettings set \
  --name my-photo-sync \
  --resource-group PhotoSyncRG \
  --settings \
    "OneDrive1:ClientId=abc123..." \
    "OneDrive1:TenantId=xyz789..." \
    "OneDrive1:ClientSecret=secret..." \
    "OneDrive1:SourceFolder=Pictures/CameraRoll" \
    "OneDrive2:ClientId=def456..." \
    "OneDrive2:TenantId=uvw012..." \
    "OneDrive2:ClientSecret=secret..." \
    "OneDrive2:SourceFolder=Pictures/CameraRoll" \
    "OneDriveDestination:ClientId=ghi789..." \
    "OneDriveDestination:TenantId=rst345..." \
    "OneDriveDestination:ClientSecret=secret..." \
    "OneDriveDestination:DestinationFolder=Pictures/FamilyPhotos"
```

## Verification

### Test the Deployed Function

Trigger it manually:
```bash
# Get the function key from Azure Portal
# Then use curl or Postman
curl -X POST https://my-photo-sync.azurewebsites.net/api/ManualSync?code=YOUR_FUNCTION_KEY
```

### Check Logs

In Azure Portal:
1. Go to your Function App
2. Click **Log stream**
3. You'll see real-time logs of the sync process

### Verify Photos

1. Check your destination OneDrive account
2. Navigate to the folder you specified (e.g., `Pictures/FamilyPhotos`)
3. You should see renamed photos with format: `20231225_143022.jpg`

## Schedule

The function runs automatically daily at 2 AM UTC. To change:
1. Open `PhotoSyncFunction.cs`
2. Change the cron expression: `[TimerTrigger("0 0 2 * * *")]`

Examples:
- Every 6 hours: `"0 0 */6 * * *"`
- Every day at 8 PM: `"0 0 20 * * *"`
- Twice daily (6 AM & 6 PM): `"0 0 6,18 * * *"`

## Troubleshooting

### "Insufficient privileges"
- Did you click **Grant admin consent** for API permissions?
- Wait 5-10 minutes after granting consent

### "Path does not exist"
- Check folder paths in settings (no leading slash!)
- Verify folders exist in OneDrive
- Use forward slashes `/` not backslashes `\`

### "Authentication failed"
- Double-check Client ID, Tenant ID, and Secret
- Make sure you copied the secret value (not the secret ID)
- Check if secret has expired

### No photos syncing
- Check that source folders have photos
- Look at the logs for specific errors
- Verify file extensions are supported (.jpg, .png, .heic, etc.)

## Cost

For typical use (500 photos/month):
- Azure Functions: ~$0.20/month
- Storage: ~$0.05/month
- Data transfer: ~$1-2/month

**Total: ~$2-3/month**

## Next Steps

- Monitor the function for a few days
- Check that photos are syncing correctly
- Adjust the schedule if needed
- Add more source accounts if needed (edit code)
- Consider organizing photos by date (see README for code example)

## Support

- Check logs in Azure Portal
- Review the full README.md for detailed documentation
- Test locally first if issues arise
