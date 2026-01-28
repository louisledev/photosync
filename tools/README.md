# PhotoSync Tools

Helper scripts for setting up and managing PhotoSync.

## get-refresh-token.js

Obtains OAuth refresh tokens for personal Microsoft accounts.

### Prerequisites

- Node.js 18+ (includes built-in `fetch`)
- Terraform deployment completed (creates the App Registration automatically)

### Usage

After running `terraform apply`, get the credentials from Terraform outputs:

```bash
# From the project root directory
node tools/get-refresh-token.js \
  $(terraform -chdir=terraform output -raw onedrive_app_client_id) \
  $(terraform -chdir=terraform output -raw onedrive_app_client_secret)
```

Or view the full command:

```bash
terraform -chdir=terraform output refresh_token_command
```

### What it does

1. Opens your default browser to Microsoft's login page
2. You sign in with the personal Microsoft account you want to authorize
3. You grant the requested permissions (Files.Read, Files.ReadWrite)
4. The script receives the authorization code via a local callback server
5. Exchanges the code for an access token and refresh token
6. Displays the refresh token (store this in `terraform.tfvars`)

### Output

The script will display:
- The refresh token (add to `terraform.tfvars`)
- The access token (valid for ~1 hour, for testing only)
- Example command to store the token via Terraform

### Troubleshooting

**Port 8080 already in use:**
- Close any applications using port 8080
- Or edit the script to use a different port (update REDIRECT_URI)

**Browser doesn't open:**
- The script will display the URL to open manually
- Copy and paste it into your browser

**Authentication fails:**
- Verify Terraform has been applied successfully
- Check that the App Registration exists in Azure Portal
- Ensure the redirect URI is configured: `http://localhost:8080/callback`

### Security Notes

- Refresh tokens are sensitive credentials - treat them like passwords
- Store them in `terraform.tfvars` (which should be in `.gitignore`)
- Terraform stores them securely in Azure Key Vault
- Refresh tokens can last up to 90 days and are automatically renewed when used
- Users can revoke access at any time via their Microsoft account settings

### Workflow

All commands should be run from the project root directory.

1. **First time setup:**
   ```bash
   # Deploy infrastructure (creates App Registration)
   az login --scope https://graph.microsoft.com/.default
   terraform -chdir=terraform apply

   # Get refresh tokens for each account
   node tools/get-refresh-token.js \
     $(terraform -chdir=terraform output -raw onedrive_app_client_id) \
     $(terraform -chdir=terraform output -raw onedrive_app_client_secret)

   # Update terraform.tfvars with the tokens, then apply again
   terraform -chdir=terraform apply
   ```

2. **Refreshing expired tokens:**
   ```bash
   # Run the script again
   node tools/get-refresh-token.js \
     $(terraform -chdir=terraform output -raw onedrive_app_client_id) \
     $(terraform -chdir=terraform output -raw onedrive_app_client_secret)

   # Update terraform.tfvars and apply
   terraform -chdir=terraform apply
   ```

See [PERSONAL_ACCOUNTS_SETUP.md](../docs/PERSONAL_ACCOUNTS_SETUP.md) for complete documentation.
