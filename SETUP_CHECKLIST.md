# Photo Sync Setup Checklist

Use this checklist to track your setup progress.

## Phase 1: Prerequisites ✓
- [ ] Azure subscription created/available
- [ ] .NET 8.0 SDK installed
- [ ] Azure Functions Core Tools installed (`npm install -g azure-functions-core-tools@4`)
- [ ] Visual Studio or VS Code with Azure Functions extension (optional but recommended)

## Phase 2: Azure AD App Registrations ✓

### OneDrive Account 1 (Your Account)
- [ ] Created app registration in Azure Portal
- [ ] Copied Client ID: `_______________________`
- [ ] Copied Tenant ID: `_______________________`
- [ ] Created client secret
- [ ] Copied Secret Value: `_______________________`
- [ ] Added Microsoft Graph permissions (Files.Read.All, Files.ReadWrite.All)
- [ ] Granted admin consent for permissions
- [ ] Verified folder path: `_______________________`

### OneDrive Account 2 (Wife's Account)
- [ ] Created app registration in Azure Portal
- [ ] Copied Client ID: `_______________________`
- [ ] Copied Tenant ID: `_______________________`
- [ ] Created client secret
- [ ] Copied Secret Value: `_______________________`
- [ ] Added Microsoft Graph permissions (Files.Read.All, Files.ReadWrite.All)
- [ ] Granted admin consent for permissions
- [ ] Verified folder path: `_______________________`

### Destination OneDrive Account
- [ ] Created app registration in Azure Portal
- [ ] Copied Client ID: `_______________________`
- [ ] Copied Tenant ID: `_______________________`
- [ ] Created client secret
- [ ] Copied Secret Value: `_______________________`
- [ ] Added Microsoft Graph permissions (Files.Read.All, Files.ReadWrite.All)
- [ ] Granted admin consent for permissions
- [ ] Verified destination folder: `_______________________`

## Phase 3: Local Configuration ✓
- [ ] Opened `local.settings.json`
- [ ] Filled in OneDrive1 credentials
- [ ] Filled in OneDrive2 credentials
- [ ] Filled in OneDriveDestination credentials
- [ ] Verified all folder paths (no leading slash, use forward slashes)
- [ ] Saved the file

## Phase 4: Local Testing ✓
- [ ] Installed Azurite (`npm install -g azurite`)
- [ ] Started Azurite in one terminal
- [ ] Ran `dotnet restore` successfully
- [ ] Ran `dotnet build` successfully (no errors)
- [ ] Started function with `func start`
- [ ] Function app started without errors
- [ ] Triggered ValidateConfig endpoint: `curl http://localhost:7071/api/ValidateConfig`
- [ ] Configuration validation passed
- [ ] Triggered ManualSync endpoint: `curl -X POST http://localhost:7071/api/ManualSync`
- [ ] Saw photos being processed in logs
- [ ] Verified photos appeared in destination OneDrive

## Phase 5: Azure Deployment ✓
- [ ] Logged into Azure (`az login`)
- [ ] Created resource group
- [ ] Created storage account
- [ ] Created Function App
- [ ] Deployed function code (`func azure functionapp publish <name>`)
- [ ] Added application settings in Azure Portal (all OneDrive credentials)
- [ ] Verified function appears in Azure Portal

## Phase 6: Production Verification ✓
- [ ] Triggered ValidateConfig endpoint in Azure
- [ ] Configuration validation passed
- [ ] Triggered ManualSync endpoint in Azure
- [ ] Checked logs in Azure Portal (Log stream or Monitor)
- [ ] Verified photos synced to destination OneDrive
- [ ] Checked Azure Table Storage for processed file records
- [ ] Verified timer trigger schedule is correct (default: 2 AM UTC)

## Phase 7: Monitoring Setup (Optional) ✓
- [ ] Enabled Application Insights
- [ ] Created dashboard in Azure Portal
- [ ] Set up alerts for function failures
- [ ] Configured log retention

## Common Issues Checklist

If something isn't working, check:
- [ ] Admin consent was granted for API permissions
- [ ] Folder paths don't have leading slashes
- [ ] Folder paths use forward slashes (/)
- [ ] Client secrets haven't expired
- [ ] All credentials are copied correctly (no spaces or missing characters)
- [ ] Source folders actually contain photos
- [ ] Waited 5-10 minutes after granting admin consent

## Success Criteria ✓

You're done when:
- [ ] Function runs locally without errors
- [ ] Photos sync from source to destination
- [ ] Files are renamed with date format (YYYYMMDD_HHMMSS.jpg)
- [ ] Function deploys to Azure successfully
- [ ] Function runs on schedule in Azure
- [ ] No duplicate processing occurs
- [ ] Logs show successful sync operations

## Notes & Important Information

Write down any custom settings or notes:

**Function App Name**: _______________________

**Resource Group Name**: _______________________

**Schedule** (if changed from default): _______________________

**Source Folder Paths**:
- Account 1: _______________________
- Account 2: _______________________

**Destination Folder Path**: _______________________

**Admin Contact**: _______________________

**Setup Date**: _______________________

**Last Verified**: _______________________

---

## Quick Reference Commands

### Local Development
```bash
# Start Azurite
azurite

# Build project
dotnet build

# Run function locally
func start

# Test config
curl http://localhost:7071/api/ValidateConfig

# Trigger sync manually
curl -X POST http://localhost:7071/api/ManualSync
```

### Azure Deployment
```bash
# Login
az login

# Deploy function
func azure functionapp publish <your-function-app-name>

# View logs
az webapp log tail --name <your-function-app-name> --resource-group <your-rg-name>
```

### Troubleshooting
```bash
# Check function status
az functionapp show --name <name> --resource-group <rg>

# Restart function
az functionapp restart --name <name> --resource-group <rg>

# View application settings
az functionapp config appsettings list --name <name> --resource-group <rg>
```
