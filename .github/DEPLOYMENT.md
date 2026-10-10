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

## .NET 10 and Flex Consumption migration

The application and both test projects target .NET 10. Build and security workflows install the .NET 10 SDK. Terraform now provisions Linux Flex Consumption (`FC1`) apps with the `dotnet-isolated` runtime version `10.0`, a private deployment blob container, and no always-ready instances. GitHub Actions deploys the prebuilt package using OneDeploy through `Azure/functions-action`.

**Migrate existing apps before enabling deployment of the .NET 10 artifact.** Linux Consumption (`Y1`) does not support .NET 10. See Microsoft's [supported versions](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#supported-versions) and [Consumption to Flex migration guide](https://learn.microsoft.com/en-us/azure/azure-functions/migration/migrate-plan-consumption-to-flex).

1. Pause automatic production deployments during migration. Confirm Flex Consumption and .NET 10 availability in your region with `az functionapp list-flexconsumption-locations` and `az functionapp list-flexconsumption-runtimes --location <region> --runtime dotnet-isolated`.
2. Back up Terraform state and retain both storage accounts, including their `ProcessedPhotos` tables. These tables prevent already synced photos from being processed again.
3. For existing apps, follow Microsoft's migration guide for each app. This configuration changes from `azurerm_linux_function_app` to `azurerm_function_app_flex_consumption`; a normal apply against the old state can delete and recreate apps. Do not treat this as a routine runtime update or use a cross-type `terraform state mv`.
4. After migrating, reconcile Terraform state: remove each old Linux Function App address from state only after the actual migration succeeds, then import the migrated Azure app ID at `module.function_app_source1.azurerm_function_app_flex_consumption.function` and `module.function_app_source2.azurerm_function_app_flex_consumption.function`. Reconcile the service plan and deployment container with the actual migrated resources as well. Alternatively, use a planned replacement with downtime; retain storage and expect new managed identities.
5. Review `terraform plan` before applying. Verify it preserves storage, uses the correct deployment containers and plans, and grants the resulting managed identities access to Key Vault. Terraform apply is a separate infrastructure step; the GitHub workflow deploys code only.
6. Update the two Function App name secrets if names changed. Deploy a successful **.NET 10** build, verify both sync functions and Application Insights, then resume automatic deployment. Manual deployment selects the latest successful main build, so ensure that build contains the upgrade.

### Recovering from a failed Consumption plan SKU update

Azure cannot update a Consumption (`Y1`/`Dynamic`) hosting plan to Flex Consumption (`FC1`). Terraform uses distinct names ending in `-flex-plan` and creates the replacement plan before deleting the old one. No manual plan creation is needed.

If an apply failed with `Cannot update ServerFarm SKU from 'Dynamic' to 'FlexConsumption'`, some other changes may already have succeeded, including deletion of the old Function Apps. Check both apps in Azure and generate a new plan from the current state; do not reuse the earlier saved plan or restore the old state backup over the partially applied state.

```bash
terraform -chdir=terraform plan -out=tfplan
terraform -chdir=terraform show -no-color tfplan
```

Review that the new plans use `-flex-plan` names and `FC1`, that both storage accounts remain intact, and that Function App deletion/creation matches your chosen replacement with downtime. If you used Microsoft's migration tooling instead, reconcile/import the resources it created before applying. Once reviewed, run `terraform -chdir=terraform apply tfplan`, then deploy application code to the new apps and verify syncing before resuming automatic deployments.

The existing 10-minute `functionTimeout` remains an application execution budget; it is not a Flex Consumption platform maximum. Flex pricing differs from legacy Consumption, so revisit cost estimates using the [Azure Functions pricing page](https://azure.microsoft.com/pricing/details/functions/).

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
- Azure Functions Flex Consumption charges for on-demand execution; no always-ready instances are configured

## Next Steps

Consider adding:
- **Staging environment** - Deploy to staging first, then production
- **Rollback mechanism** - Keep previous deployment slots
- **Notification integration** - Slack/Teams alerts on deployment
- **Infrastructure deployment** - Add Terraform apply to workflow
