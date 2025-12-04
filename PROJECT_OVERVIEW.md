# Photo Sync Project - File Structure

This project contains a complete Azure Functions solution for automated photo synchronization from multiple OneDrive accounts.

## 📁 Project Files

### Core Application Files
- **PhotoSyncFunction.cs** - Main timer-triggered function that runs daily
- **PhotoSyncService.cs** - Business logic for downloading, renaming, and uploading photos
- **StateManager.cs** - Tracks processed files using Azure Table Storage
- **Program.cs** - Application startup and dependency injection configuration
- **ManualTrigger.cs** - HTTP endpoint for manually triggering sync
- **ValidateConfig.cs** - HTTP endpoint for validating configuration
- **ConfigurationValidator.cs** - Helper class to validate OneDrive credentials and folders

### Configuration Files
- **PhotoSync.csproj** - Project file with NuGet package references
- **PhotoSync.sln** - Visual Studio solution file
- **host.json** - Azure Functions host configuration
- **local.settings.json** - Local development settings (add your credentials here)
- **.gitignore** - Git ignore rules (prevents committing secrets)

### Documentation
- **README.md** - Complete documentation with setup instructions
- **QUICKSTART.md** - Fast 30-minute getting started guide

### Deployment Scripts
- **deploy.ps1** - PowerShell deployment script for Windows
- **deploy.sh** - Bash deployment script for Mac/Linux

## 🚀 Quick Start

1. **Start here**: Read `QUICKSTART.md`
2. **Configure**: Edit `local.settings.json` with your OneDrive credentials
3. **Test locally**: Run `func start` 
4. **Deploy**: Use `deploy.ps1` or `deploy.sh`
5. **Verify**: Use the ValidateConfig endpoint

## 📋 What You Need to Provide

Before running, you need to:

1. **Register 3 Azure AD Applications** (see QUICKSTART.md)
   - One for each source OneDrive account
   - One for the destination OneDrive account

2. **Fill in credentials** in `local.settings.json`:
   - Client IDs
   - Tenant IDs
   - Client Secrets
   - Folder paths

## 🔧 Key Features

- **Automatic scheduling**: Runs daily at 2 AM UTC
- **Smart renaming**: Uses EXIF metadata to name files `YYYYMMDD_HHMMSS.jpg`
- **Duplicate detection**: Won't re-process the same photos
- **Large file support**: Handles files > 4MB with chunked upload
- **Multiple formats**: JPG, PNG, HEIC, RAW, and more
- **Manual triggering**: HTTP endpoint for on-demand sync
- **Configuration validation**: Test your setup before running

## 📊 Architecture

**Two Separate Function Apps for Complete Isolation:**

```
Source OneDrive #1 (Your photos)
       ↓
Azure Function App #1 (Timer: Daily at 2 AM)
       ↓
Download → Extract EXIF → Rename → Upload
       ↓
Destination OneDrive (Family photos)

Source OneDrive #2 (Wife's photos)
       ↓
Azure Function App #2 (Timer: Daily at 2 AM)
       ↓
Download → Extract EXIF → Rename → Upload
       ↓
Destination OneDrive (Family photos)
       ↓
Azure Table Storage (Track processed files)
```

### Why Two Function Apps?

1. **Complete Isolation**: One source account's issues won't affect the other
2. **Independent Scaling**: Scale each based on its workload
3. **Better Security**: Each app only has credentials for its source account
4. **Easier Monitoring**: Separate logs and metrics per source
5. **Same Cost**: Consumption Plan charges per execution, not per app

## 💰 Estimated Cost

For typical usage (500 photos/month with 2 Function Apps):
- Azure Functions (2 apps): ~$0.40/month
- Azure Storage (2 accounts): ~$0.10/month
- Data Transfer: ~$1-2/month
- **Total: ~$2.50-$3/month**

## 📖 Next Steps

1. Read `QUICKSTART.md` for setup
2. Configure `local.settings.json`
3. Test locally
4. Deploy to Azure
5. Monitor logs in Azure Portal

## 🆘 Need Help?

- Check the **Troubleshooting** section in README.md
- Use the **ValidateConfig** endpoint to test your setup
- Review Azure Function logs in the Portal

## 📝 License

Personal use. Modify as needed.
