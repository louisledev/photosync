using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace PhotoSync
{
    /// <summary>
    /// Factory for creating Microsoft Graph clients using refresh token authentication
    /// for personal Microsoft accounts
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

        public GraphServiceClient CreateClient(string clientId, string refreshTokenSecretName, string clientSecretName)
        {
            // Always use refresh token authentication for personal Microsoft accounts
            var refreshToken = GetRefreshToken(refreshTokenSecretName);
            var authProvider = new RefreshTokenAuthenticationProvider(clientId, GetClientSecret(clientSecretName), refreshToken, _httpClient);
            return new GraphServiceClient(authProvider);
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateRefreshTokenAsync(
            string clientId,
            string refreshTokenSecretName,
            string clientSecretName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var refreshToken = GetRefreshToken(refreshTokenSecretName);
                var authProvider = new RefreshTokenAuthenticationProvider(clientId, GetClientSecret(clientSecretName), refreshToken, _httpClient);
                return await authProvider.ValidateRefreshTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to validate token for {refreshTokenSecretName}: {ex.Message}");
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

        private string GetClientSecret(string keyName)
        {
            if (_secretClient == null)
            {
                throw new InvalidOperationException("Key Vault is not configured. Ensure 'KeyVault:VaultUrl' is set in configuration and Key Vault is enabled in Terraform (enable_keyvault = true).");
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new ArgumentNullException(nameof(keyName), "Client secret name must be provided.");
            }

            try
            {
                var secret = _secretClient.GetSecret(keyName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve client secret '{keyName}' from Key Vault: {ex.Message}", ex);
            }
        }
    }
}
