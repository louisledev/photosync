# Terraform Configuration for PhotoSync

Infrastructure as Code for deploying PhotoSync to Azure.

## Quick Start

### Initial Setup (Local State)

```bash
# 1. Login to Azure
az login

# 2. Configure variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# 3. Initialize and deploy
terraform init
terraform plan
terraform apply
```

### Remote State Setup (Recommended for Multi-Machine Access)

```bash
# 1. Create Azure Storage for state
./setup-remote-state.sh

# 2. Enable remote backend
# Edit backend.tf and uncomment the backend block

# 3. Migrate state to Azure
terraform init -migrate-state
```

## Documentation

- **[TERRAFORM.md](TERRAFORM.md)** - Complete deployment guide
- **[backend.tf](backend.tf)** - Remote state configuration
- **[setup-remote-state.sh](setup-remote-state.sh)** - Backend setup script

## Files

- `main.tf` - Main infrastructure resources
- `variables.tf` - Variable declarations
- `outputs.tf` - Output values
- `backend.tf` - Remote state backend configuration
- `terraform.tfvars` - Your actual values (not in git)
- `modules/` - Reusable Terraform modules

## Cost

Estimated monthly cost: ~$2.50-3.00 (with Consumption plan)
- Remote state storage: +$0.10/month (if enabled)
