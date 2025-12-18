# PhotoSync Tools

Helper scripts for setting up and managing PhotoSync.

## get-refresh-token.js

Obtains OAuth refresh tokens for personal Microsoft accounts.

### Prerequisites

- Node.js 18+ (includes built-in `fetch`)
- Azure AD app registration with redirect URI: `http://localhost:8080/callback`
- Client ID and Client Secret from your app registration

### Usage

```bash
node get-refresh-token.js <CLIENT_ID> <CLIENT_SECRET>
```

### Example

```bash
node get-refresh-token.js abc123-456-789 MySecret123
```

### What it does

1. Opens your default browser to Microsoft's login page
2. You sign in with the personal Microsoft account you want to authorize
3. You grant the requested permissions (Files.Read, Files.ReadWrite)
4. The script receives the authorization code via a local callback server
5. Exchanges the code for an access token and refresh token
6. Displays the refresh token (store this in Azure Key Vault)

### Output

The script will display:
- ✅ The refresh token (store in Azure Key Vault)
- ℹ️  The access token (valid for ~1 hour, for testing only)
- 📋 Example command to store the token in Key Vault

### Troubleshooting

**Port 8080 already in use:**
- Close any applications using port 8080
- Or edit the script to use a different port (update REDIRECT_URI)

**Browser doesn't open:**
- The script will display the URL to open manually
- Copy and paste it into your browser

**Authentication fails:**
- Verify your Client ID and Client Secret are correct
- Ensure the redirect URI is configured in your app registration: `http://localhost:8080/callback`
- Check that your app has delegated permissions (not application permissions):
  - Files.Read
  - Files.ReadWrite
  - offline_access

### Security Notes

- Refresh tokens are sensitive credentials - treat them like passwords
- Store them in Azure Key Vault, not in configuration files
- Refresh tokens can last up to 90 days and are automatically renewed when used
- Users can revoke access at any time via their Microsoft account settings

### Next Steps

After obtaining the refresh token:

1. **Store in Azure Key Vault:**
   ```bash
   az keyvault secret set \
     --vault-name your-vault-name \
     --name source1-refresh-token \
     --value "YOUR_REFRESH_TOKEN_HERE"
   ```

2. **Configure Function App:**
   Add these settings to your Function App:
   ```bash
   az functionapp config appsettings set \
     --name your-function-app \
     --resource-group PhotoSyncRG \
     --settings \
       "UseRefreshTokenAuth=true" \
       "KeyVault:VaultUrl=https://your-vault-name.vault.azure.net/" \
       "OneDriveSource:ClientId=your-client-id" \
       "OneDriveSource:RefreshTokenSecretName=source1-refresh-token" \
       "OneDriveSource:ClientSecretName=source1-client-secret"
   ```

3. **Enable Managed Identity:**
   ```bash
   # Enable system-assigned managed identity
   az functionapp identity assign \
     --name your-function-app \
     --resource-group PhotoSyncRG

   # Grant Key Vault access
   PRINCIPAL_ID=$(az functionapp identity show \
     --name your-function-app \
     --resource-group PhotoSyncRG \
     --query principalId -o tsv)

   az keyvault set-policy \
     --name your-vault-name \
     --object-id $PRINCIPAL_ID \
     --secret-permissions get list
   ```

See [PERSONAL_ACCOUNTS_SETUP.md](../docs/PERSONAL_ACCOUNTS_SETUP.md) for complete documentation.
