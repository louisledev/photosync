# 📸 Photo Sync Azure Function - START HERE

Welcome! This project automates syncing family photos from multiple OneDrive accounts into one consolidated location.

## 🎯 What This Does

- Deploys **two separate Azure Function Apps** for complete isolation
- Each Function App syncs from one OneDrive source account
- Automatically downloads photos daily at 2 AM UTC
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

1. Read [README.md](README.md) for comprehensive documentation
2. Use [SETUP_CHECKLIST.md](SETUP_CHECKLIST.md) to track your progress
3. Refer back as needed

## 📁 Important Files (What to Look At First)

### Must Read
- **PERSONAL_ACCOUNTS_SETUP.md** - ⚠️ START HERE if using personal Microsoft accounts (outlook.com, hotmail.com, etc.)
- **ARCHITECTURE_CHANGES.md** - Understand the two Function App design
- **PROJECT_OVERVIEW.md** - Architecture overview and benefits
- **terraform/** - Infrastructure deployment (Terraform)
- **.github/DEPLOYMENT.md** - CI/CD with GitHub Actions

### Reference Documentation
- **README.md** - Complete documentation (organizational accounts)
- **QUICKSTART.md** - Setup guide (local development)

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
2. **.NET 8.0 SDK** installed
3. **Terraform** ([Download here](https://www.terraform.io/downloads))
4. **Azure CLI** ([Download here](https://docs.microsoft.com/cli/azure/install-azure-cli))
5. Access to the OneDrive accounts you want to sync

## 🎬 Your First Steps

### Option A: Production Deployment (Recommended)

1. **Set up Azure AD apps** (15 minutes)
   - You need 3 app registrations (one for each OneDrive account)
   - See [QUICKSTART.md](QUICKSTART.md) for detailed steps

2. **Configure Terraform** (5 minutes)
   ```bash
   cd terraform
   cp terraform.tfvars.example terraform.tfvars
   # Edit terraform.tfvars with your credentials
   ```

3. **Deploy infrastructure** (5 minutes)
   ```bash
   terraform init
   terraform plan
   terraform apply
   ```

4. **Deploy code to both Function Apps** (5 minutes)
   ```bash
   # Get Function App names
   SOURCE1=$(terraform output -raw function_app_source1_name)
   SOURCE2=$(terraform output -raw function_app_source2_name)

   # Deploy
   cd ../src
   func azure functionapp publish $SOURCE1
   func azure functionapp publish $SOURCE2
   ```

### Option B: Local Development

1. **Configure local settings** (5 minutes)
   - Copy `src/local.settings.json.example` to `src/local.settings.json`
   - Add your OneDrive credentials using `OneDriveSource` and `OneDriveDestination`

2. **Test locally** (5 minutes)
   ```bash
   # Start storage emulator
   npm install -g azurite
   azurite

   # In another terminal
   cd src
   dotnet restore
   dotnet build
   func start
   ```

## 🔧 Testing Your Setup

### Validate Configuration
```bash
# Local
curl http://localhost:7071/api/ValidateConfig

# Azure (after deployment)
curl https://your-function-app.azurewebsites.net/api/ValidateConfig?code=YOUR_KEY
```

### Manual Sync
```bash
# Local
curl -X POST http://localhost:7071/api/ManualSync

# Azure
curl -X POST https://your-function-app.azurewebsites.net/api/ManualSync?code=YOUR_KEY
```

## 📅 Scheduling

The function runs automatically **daily at 2 AM UTC**.

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
A: ~$2.50-3/month for typical usage (500 photos/month with 2 Function Apps)

**Q: Will it create duplicates?**
A: No, it tracks processed files in Azure Table Storage

**Q: Can I add more source accounts?**
A: Yes! Deploy additional Function Apps using the Terraform module. See [ARCHITECTURE_CHANGES.md](ARCHITECTURE_CHANGES.md)

**Q: Why two separate Function Apps?**
A: Complete isolation, independent scaling, better security. Each app only has credentials for its source account.

**Q: What photo formats are supported?**
A: JPG, PNG, HEIC, HEIF, RAW, CR2, NEF, and more

**Q: Can I organize photos by date/folder?**
A: Yes! See README.md for customization examples

## 🆘 Need Help?

1. Check **README.md** Troubleshooting section
2. Run the ValidateConfig endpoint
3. Review logs in Azure Portal
4. Common issues are usually:
   - Missing admin consent for API permissions
   - Incorrect folder paths (check slashes!)
   - Expired client secrets

## 📊 Project Structure

```
PhotoSync/
├── START_HERE.md              ← You are here!
├── ARCHITECTURE_CHANGES.md    ← Two Function App design explained
├── PROJECT_OVERVIEW.md        ← Architecture overview
├── README.md                  ← Complete documentation
├── QUICKSTART.md              ← Local development setup
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
- ✅ Both functions run on schedule (daily at 2 AM UTC)

## 🎉 Ready to Begin?

**Choose your path:**
1. **Production**: Read [ARCHITECTURE_CHANGES.md](ARCHITECTURE_CHANGES.md) then deploy with Terraform
2. **Local Development**: Open [QUICKSTART.md](QUICKSTART.md) to test locally first
3. **CI/CD**: See [.github/DEPLOYMENT.md](.github/DEPLOYMENT.md) for automated deployments

---

*Built with ❤️ for automated family photo management*
*Now with complete isolation via two Function App architecture!*
