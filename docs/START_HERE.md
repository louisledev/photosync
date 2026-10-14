# 📸 Photo Sync Azure Function - START HERE

Welcome! This project automates syncing family photos from multiple OneDrive accounts into one consolidated location.

## 🎯 What This Does

- Deploys **two separate Azure Function Apps** for complete isolation
- Each Function App syncs from one OneDrive source account
- Automatically downloads photos every hour
- Renames photos based on EXIF date/time metadata
- Uploads to a single destination OneDrive
- Prevents duplicate processing
- Costs ~$2.50-3/month to run

## 🚀 Getting Started (Choose Your Path)

### Path 1: Quick Start (Recommended) ⚡
**Time: ~30 minutes**

1. Read [QUICKSTART.md](QUICKSTART.md)
2. Follow the step-by-step guide
3. You'll be syncing photos in no time!

### Path 2: Detailed Setup 📚
**Time: ~1 hour (includes understanding everything)**

1. Read [README.md](../README.md) for comprehensive documentation
2. Use [SETUP_CHECKLIST.md](SETUP_CHECKLIST.md) to track your progress
3. Refer back as needed

## 📁 Important Files (What to Look At First)

### Must Read
- **QUICKSTART.md** - ⚠️ START HERE for a 30-minute setup guide
- **PERSONAL_ACCOUNTS_SETUP.md** - Detailed documentation for personal Microsoft accounts
- **PROJECT_OVERVIEW.md** - Architecture overview and two Function App design
- **terraform/** - Infrastructure deployment (Terraform)

### Reference Documentation
- **README.md** - Complete documentation and customization options (in root folder)
- **terraform/TERRAFORM.md** - Infrastructure deployment guide
- **.github/DEPLOYMENT.md** - CI/CD with GitHub Actions

### Core Application Code
- **src/PhotoSyncFunction.cs** - Main sync logic
- **src/StateManager.cs** - Tracks processed files
- **src/ConfigurationValidator.cs** - Validates your setup

### Deployment
- **terraform/** - Infrastructure as Code (recommended)
- **.github/workflows/deploy.yml** - Automated CI/CD

## ✅ Prerequisites

Before you start, make sure you have:

1. **Azure subscription** ([Free trial available](https://azure.microsoft.com/free/))
2. **.NET 10 SDK** installed
3. **Terraform** ([Download here](https://www.terraform.io/downloads))
4. **Azure CLI** ([Download here](https://docs.microsoft.com/cli/azure/install-azure-cli))
5. Access to the OneDrive accounts you want to sync

## 🎬 Your First Steps

### Quick Setup (Recommended - 30 minutes)

Follow [QUICKSTART.md](QUICKSTART.md) for a complete guided setup:

1. **Register ONE Azure AD app** (5 min)
   - One app registration works for all personal accounts
   - See [QUICKSTART.md](QUICKSTART.md) Step 1

2. **Get refresh tokens** (10 min)
   - Run `tools/get-refresh-token.js` for each account
   - Save the tokens securely
   - See [QUICKSTART.md](QUICKSTART.md) Step 2

3. **Configure and deploy with Terraform** (10 min)
   ```bash
   cd terraform
   cp terraform.tfvars.example terraform.tfvars
   # Edit with your client ID, tokens, and settings
   terraform init
   terraform apply
   ```

4. **Deploy code** (3 min)
   ```bash
   SOURCE1=$(terraform output -raw function_app_source1_name)
   SOURCE2=$(terraform output -raw function_app_source2_name)

   cd src
   func azure functionapp publish $SOURCE1
   func azure functionapp publish $SOURCE2
   ```

## 🔧 Testing Your Setup

### Check Logs
```bash
# View real-time logs
az functionapp log tail --name $SOURCE1 --resource-group PhotoSyncRG
```

### Manual Trigger (Optional)
```bash
# Trigger immediately without waiting for schedule
az functionapp function invoke \
  --name $SOURCE1 \
  --resource-group PhotoSyncRG \
  --function-name PhotoSyncTimer
```

### Verify Photos
1. Sign in to destination OneDrive at onedrive.com
2. Navigate to your destination folder
3. Look for photos organized by date: `2025/2025-12/20231225_143022.jpg`

## 📅 Scheduling

The function runs automatically **every hour**.

To change the schedule, edit `PhotoSyncFunction.cs`:
```csharp
[TimerTrigger("0 0 2 * * *")] // second minute hour day month dayOfWeek
```

Examples:
- Every 6 hours: `"0 0 */6 * * *"`
- Every day at noon: `"0 0 12 * * *"`
- Twice daily: `"0 0 6,18 * * *"`

## 🔍 Monitoring

After deployment, monitor your function:

1. **Azure Portal** → Your Function App → **Log stream**
2. **Application Insights** → View metrics and errors
3. **Azure Table Storage** → See processed file records

## ❓ Common Questions

**Q: How much will this cost?**
A: ~$2.50-3/month for typical usage (500 photos/month with 2 Function Apps + Key Vault)

**Q: Why personal accounts only?**
A: This project is designed specifically for personal Microsoft accounts using refresh token authentication. Corporate accounts would require different permissions and setup.

**Q: Will it create duplicates?**
A: No, it tracks processed files in Azure Table Storage and uses OneDrive's auto-rename feature

**Q: Can I add more source accounts?**
A: Yes! Deploy additional Function Apps using the Terraform module. See [PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md)

**Q: Why two separate Function Apps?**
A: Complete isolation, independent scaling, better security. Each app only has credentials for its source account.

**Q: What photo formats are supported?**
A: JPG, PNG, HEIC, HEIF, RAW, CR2, NEF, MP4, MOV, and more

**Q: How are photos organized?**
A: By date in folders like `2025/2025-12/` with filenames like `20231225_143022.jpg`

## 🆘 Need Help?

1. Check [PERSONAL_ACCOUNTS_SETUP.md](PERSONAL_ACCOUNTS_SETUP.md) Troubleshooting section
2. Check [QUICKSTART.md](QUICKSTART.md) Troubleshooting section
3. Review logs in Azure Portal: `az functionapp log tail`
4. Common issues:
   - Refresh token expired → Re-run `get-refresh-token.js`
   - Key Vault access denied → Check managed identity has permissions
   - Wrong tenant ID → Use `common` not your actual tenant ID
   - Folder paths incorrect → Use `/Photos` not `Photos` or `\Photos`

## 📊 Project Structure

```
PhotoSync/
├── README.md                  ← Complete documentation (root)
├── docs/
│   ├── START_HERE.md          ← You are here!
│   ├── PROJECT_OVERVIEW.md    ← Architecture overview
│   ├── QUICKSTART.md          ← Quick start guide
│   ├── PERSONAL_ACCOUNTS_SETUP.md ← Detailed setup
│   └── ...                    ← Other documentation
│
├── src/                       ← C# Source Code
│   ├── PhotoSyncFunction.cs   ← Main sync logic
│   ├── StateManager.cs        ← State tracking
│   ├── ConfigurationValidator.cs ← Config validation
│   ├── ManualTrigger.cs       ← Manual sync endpoint
│   ├── ValidateConfig.cs      ← Validation endpoint
│   ├── Program.cs             ← App startup
│   ├── PhotoSync.csproj       ← Project file
│   └── host.json              ← Function host config
│
├── terraform/                 ← Infrastructure as Code
│   ├── main.tf                ← Main configuration
│   ├── variables.tf           ← Variable definitions
│   ├── outputs.tf             ← Outputs (Function App names)
│   ├── terraform.tfvars.example ← Example configuration
│   └── modules/function-app/  ← Reusable Function App module
│
├── .github/workflows/         ← CI/CD
│   └── deploy.yml             ← Automated deployment
│
└── tests/                     ← Unit & Integration Tests
    ├── PhotoSync.Tests/       ← 74 unit tests
    └── PhotoSync.IntegrationTests/ ← 9 integration tests
```

## 🎓 Learning Resources

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Microsoft Graph API](https://docs.microsoft.com/graph/)
- [Azure Storage Tables](https://docs.microsoft.com/azure/storage/tables/)

## 🚦 Status Indicators

After setup, you should see:
- ✅ Two Function Apps deployed (photosync-source1, photosync-source2)
- ✅ Terraform outputs show both app names and URLs
- ✅ Photos appear in destination OneDrive
- ✅ Files renamed with date format (YYYYMMDD_HHMMSS.jpg)
- ✅ No errors in Azure logs for either app
- ✅ Both functions run on schedule (every hour)

## 🎉 Ready to Begin?

**Recommended path for personal Microsoft accounts:**

1. **Quick Start**: Follow [QUICKSTART.md](QUICKSTART.md) for step-by-step setup (30 minutes)
2. **Detailed Docs**: Read [PERSONAL_ACCOUNTS_SETUP.md](PERSONAL_ACCOUNTS_SETUP.md) for comprehensive documentation
3. **Architecture**: See [PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md) to understand the two Function App design
4. **CI/CD**: See [.github/DEPLOYMENT.md](.github/DEPLOYMENT.md) for automated deployments (optional)

---

*Built with ❤️ for automated family photo management*
*Using refresh token authentication for personal Microsoft accounts*
*Complete isolation via two Function App architecture!*
