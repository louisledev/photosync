using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace PhotoSync
{
    /// <summary>
    /// Factory for creating Microsoft Graph clients with support for both
    /// client credentials (organizational accounts) and refresh tokens (personal accounts)
    /// </summary>
    public class GraphClientFactory : IGraphClientFactory
    {
        private readonly IConfiguration _configuration;
        private readonly SecretClient? _secretClient;
        private readonly HttpClient _httpClient;

        public GraphClientFactory(IConfiguration configuration, HttpClient? httpClient = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? new HttpClient();

            // Initialize Key Vault client if URL is configured
            var keyVaultUrl = _configuration["KeyVault:VaultUrl"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                var credential = new DefaultAzureCredential();
                _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
            }
        }

        public GraphServiceClient CreateClient(string clientId, string tenantId, string clientSecret)
        {
            // Check if we're using refresh token mode
            var useRefreshToken = bool.TryParse(_configuration["UseRefreshTokenAuth"], out var useRefresh) && useRefresh;

            if (useRefreshToken)
            {
                // Use refresh token authentication for personal Microsoft accounts
                var refreshToken = GetRefreshToken(clientSecret); // clientSecret parameter is used as refresh token key name
                var authProvider = new RefreshTokenAuthenticationProvider(clientId, GetClientSecret(clientId), refreshToken, _httpClient);
                return new GraphServiceClient(authProvider);
            }
            else
            {
                // Use client credentials flow for organizational accounts
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                return new GraphServiceClient(credential);
            }
        }

        private string GetRefreshToken(string keyName)
        {
            if (_secretClient == null)
            {
                throw new InvalidOperationException("Key Vault is not configured. Set KeyVault:VaultUrl in configuration.");
            }

            try
            {
                var secret = _secretClient.GetSecret(keyName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve refresh token '{keyName}' from Key Vault: {ex.Message}", ex);
            }
        }

        private string GetClientSecret(string clientId)
        {
            // Try to get client secret from Key Vault first, fall back to configuration
            if (_secretClient != null)
            {
                try
                {
                    var secretName = $"{clientId}-client-secret";
                    var secret = _secretClient.GetSecret(secretName);
                    return secret.Value.Value;
                }
                catch
                {
                    // Fall through to configuration
                }
            }

            // Look for client secret in configuration
            var clientSecret = _configuration[$"OneDriveSource:ClientSecret"] ?? _configuration[$"OneDriveDestination:ClientSecret"];
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException($"Client secret not found for client ID: {clientId}");
            }

            return clientSecret;
        }
    }
}
