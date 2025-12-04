# GitHub Actions Deployment Setup

This document explains how to set up automated CI/CD for the PhotoSync application using GitHub Actions.

## Architecture Overview

PhotoSync uses a **two Function App architecture**:
- **Function App Source 1**: Handles syncing from the first OneDrive source account
- **Function App Source 2**: Handles syncing from the second OneDrive source account
- **Same codebase**: Both Function Apps run the same code, but with different configuration

This provides complete isolation, independent scaling, and better security.

## Workflow Overview

The deployment workflow ([.github/workflows/deploy.yml](workflows/deploy.yml)) automates:

1. **Build and Test** (runs on every push and PR)
   - Restores NuGet dependencies
   - Builds the solution in Release configuration
   - Runs unit tests (PhotoSync.Tests)
   - Runs integration tests (PhotoSync.IntegrationTests)
   - Publishes test results
   - Creates deployment artifact

2. **Deploy** (runs only on push to main branch)
   - Downloads the build artifact
   - Authenticates with Azure
   - Deploys the same code to both Function Apps

## Prerequisites

Before enabling the workflow, you need:

1. **Two Azure Function Apps** - Already provisioned via Terraform
2. **Azure Service Principal** - For GitHub Actions to authenticate with Azure
3. **GitHub Repository Secrets** - To store credentials securely

## Setup Instructions

### Step 1: Create Azure Service Principal

Run this command in Azure CLI to create a service principal with Contributor access to your resource group:

```bash
az ad sp create-for-rbac \
  --name "github-actions-photosync" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group-name} \
  --sdk-auth
```

**Important:** Replace `{subscription-id}` and `{resource-group-name}` with your actual values.

This command outputs JSON credentials. Copy the entire JSON output - you'll need it in the next step.

Example output:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

### Step 2: Configure GitHub Secrets

Add the following secrets to your GitHub repository:

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** and add:

   - **Secret Name:** `AZURE_CREDENTIALS`
     - **Value:** Paste the entire JSON output from Step 1

   - **Secret Name:** `AZURE_FUNCTIONAPP_SOURCE1_NAME`
     - **Value:** Your Function App Source 1 name (from `terraform output function_app_source1_name`)

   - **Secret Name:** `AZURE_FUNCTIONAPP_SOURCE2_NAME`
     - **Value:** Your Function App Source 2 name (from `terraform output function_app_source2_name`)

### Step 3: Verify Terraform Outputs

Get your Function App names from Terraform:

```bash
cd terraform
terraform output function_app_source1_name
terraform output function_app_source2_name
```

Use these values for the `AZURE_FUNCTIONAPP_SOURCE1_NAME` and `AZURE_FUNCTIONAPP_SOURCE2_NAME` secrets.

### Step 4: Enable GitHub Actions

1. Push the `.github/workflows/deploy.yml` file to your repository
2. Go to the **Actions** tab in your GitHub repository
3. You should see the workflow appear

### Step 5: Test the Workflow

**Option A: Push to main branch**
```bash
git add .
git commit -m "Add GitHub Actions workflow"
git push origin main
```

**Option B: Manual trigger**
1. Go to **Actions** tab
2. Select "Build and Deploy to Azure Functions"
3. Click **Run workflow** → **Run workflow**

## Workflow Behavior

### On Pull Requests
- Builds the code
- Runs all tests (unit + integration)
- Reports test results
- **Does NOT deploy** to Azure

### On Push to Main
- Builds the code
- Runs all tests
- Deploys to Azure Functions (only if tests pass)
- Uses the `production` environment

### Manual Trigger
- Can be triggered from the Actions tab
- Runs the full build, test, and deploy pipeline

## Monitoring Deployments

### View Workflow Status
1. Go to **Actions** tab in GitHub
2. Click on a workflow run to see detailed logs
3. Each step shows execution time and output

### View Azure Deployment
After successful deployment, verify in Azure:
```bash
# Check function app status
az functionapp show --name <function-app-name> --resource-group <resource-group-name>

# View recent deployments
az functionapp deployment list --name <function-app-name> --resource-group <resource-group-name>

# Stream logs
az functionapp log tail --name <function-app-name> --resource-group <resource-group-name>
```

## Troubleshooting

### Authentication Failed
- Verify `AZURE_CREDENTIALS` secret contains valid JSON
- Check service principal has Contributor role on the resource group
- Ensure subscription ID and tenant ID are correct

### Deployment Failed
- Check `AZURE_FUNCTIONAPP_NAME` matches your actual Function App name
- Verify Function App exists: `az functionapp list --resource-group <rg-name>`
- Check Function App is running: `az functionapp show --name <app-name> --resource-group <rg-name> --query state`

### Tests Failed
- Review test output in the Actions tab
- Integration tests require Docker (provided by GitHub Actions runners)
- Check for dependency issues in the build logs

## Environment Protection (Optional)

To add manual approval before production deployment:

1. Go to **Settings** → **Environments** → **production**
2. Enable **Required reviewers**
3. Add team members who can approve deployments
4. Configure environment secrets if needed

This adds a manual gate before deploying to Azure.

## Cost Considerations

- GitHub Actions provides **2,000 free minutes/month** for private repositories
- This workflow typically uses **5-8 minutes** per run
- Azure Functions Consumption Plan charges per execution (already configured)

## Next Steps

Consider adding:
- **Staging environment** - Deploy to staging first, then production
- **Rollback mechanism** - Keep previous deployment slots
- **Notification integration** - Slack/Teams alerts on deployment
- **Infrastructure deployment** - Add Terraform apply to workflow
