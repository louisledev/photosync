# Terraform Deployment Guide for Photo Sync

This guide explains how to deploy the Photo Sync Azure Function using Terraform.

## Prerequisites

1. **Install Terraform**: Download from [terraform.io](https://www.terraform.io/downloads)
2. **Install Azure CLI**: Download from [Microsoft Docs](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
3. **Azure Subscription**: You need an active Azure subscription
4. **Azure Functions Core Tools**: For deploying the function code

## Setup

### 1. Login to Azure

```bash
az login
```

### 2. Configure Variables

Copy the example variables file and edit it with your values:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` and provide:
- Resource group name
- Function app name
- Location (default: westeurope)
- OneDrive credentials for all three accounts

**Note**: The `terraform.tfvars` file contains sensitive information. Make sure it's in your `.gitignore` file.

## Deployment

### 1. Initialize Terraform

```bash
terraform init
```

This downloads the required providers (Azure and Random).

### 2. Plan the Deployment

```bash
terraform plan
```

Review the resources that will be created:
- Resource Group
- Storage Account
- App Service Plan (Consumption)
- Linux Function App

### 3. Apply the Configuration

```bash
terraform apply
```

Type `yes` when prompted to confirm the deployment.

### 4. Deploy the Function Code

After Terraform creates the infrastructure, deploy your function code:

```bash
func azure functionapp publish $(terraform output -raw function_app_name)
```

Or use the deployment command from the output:

```bash
terraform output deployment_command
```

## Managing the Infrastructure

### View Current State

```bash
terraform show
```

### View Outputs

```bash
terraform output
```

### Update Configuration

1. Modify your `terraform.tfvars` or `.tf` files
2. Run `terraform plan` to see changes
3. Run `terraform apply` to apply changes

### Update Application Settings Only

If you only need to update OneDrive credentials or other app settings:

```bash
terraform apply -target=azurerm_linux_function_app.photosync
```

### Destroy Resources

To remove all resources created by Terraform:

```bash
terraform destroy
```

## File Structure

- **main.tf**: Main infrastructure configuration
- **variables.tf**: Variable declarations
- **outputs.tf**: Output values after deployment
- **terraform.tfvars**: Your actual values (not committed to git)
- **terraform.tfvars.example**: Example configuration

## Configuration Details

### Resource Naming

- **Resource Group**: Uses the name you specify in `resource_group_name`
- **Storage Account**: Combines `storage_account_prefix` with a random 5-digit number
- **Function App**: Uses the name you specify in `function_app_name`
- **App Service Plan**: Named `{function_app_name}-plan`

### Application Settings

The following app settings are automatically configured:
- OneDrive1 credentials and source folder
- OneDrive2 credentials and source folder
- OneDriveDestination credentials and destination folder
- Required Azure Functions runtime settings

### Cost Optimization

The configuration uses:
- **Consumption Plan (Y1)**: Pay only for execution time
- **Standard_LRS Storage**: Locally redundant storage for cost efficiency

## Troubleshooting

### Storage Account Name Conflicts

If you get a storage account name conflict, Terraform will automatically generate a new random suffix on the next apply.

### Function App Name Already Taken

Function app names must be globally unique. If your chosen name is taken, update the `function_app_name` in `terraform.tfvars`.

### Authentication Issues

Ensure you're logged in to Azure CLI and have sufficient permissions:

```bash
az account show
az account list
```

### Terraform State

By default, Terraform stores state locally in `terraform.tfstate`. This file contains sensitive information and should not be committed to version control.

**For multi-machine access or team environments**, use Azure Storage as a remote backend (recommended):

#### Setup Remote State Backend

1. **Run the setup script**:
   ```bash
   cd terraform
   ./setup-remote-state.sh
   ```

   This creates:
   - Azure Storage Account for state storage
   - Blob container with versioning enabled
   - Secure configuration (HTTPS-only, no public access)

2. **Edit `backend.tf`**:
   - Uncomment the `backend "azurerm"` block
   - Update `storage_account_name` if you changed it (must be globally unique)

3. **Migrate existing state** (if you have local state):
   ```bash
   terraform init -migrate-state
   ```

   Answer `yes` when prompted to migrate state to Azure Storage.

4. **Or initialize fresh** (if no existing state):
   ```bash
   terraform init
   ```

#### Benefits of Remote State:
- ✅ Access from multiple machines
- ✅ Automatic state locking (prevents concurrent modifications)
- ✅ State versioning and backup
- ✅ Secure storage with encryption
- ✅ Team collaboration support

## Security Best Practices

1. **Never commit `terraform.tfvars`** - Add it to `.gitignore`
2. **Use Azure Key Vault** - For production, consider storing secrets in Key Vault
3. **Use Managed Identities** - Where possible, use managed identities instead of client secrets
4. **Restrict Access** - Use Azure RBAC to control who can deploy

## Additional Resources

- [Terraform Azure Provider Documentation](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Azure Functions on Linux](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli?tabs=linux)
- [Terraform Best Practices](https://www.terraform.io/docs/cloud/guides/recommended-practices/index.html)
