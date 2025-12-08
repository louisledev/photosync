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

        public GraphServiceClient CreateClient(string clientId, string tenantId, string refreshTokenSecretName)
        {
            // Always use refresh token authentication for personal Microsoft accounts
            var refreshToken = GetRefreshToken(refreshTokenSecretName);
            var authProvider = new RefreshTokenAuthenticationProvider(clientId, GetClientSecret(clientId), refreshToken, _httpClient);
            return new GraphServiceClient(authProvider);
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateRefreshTokenAsync(
            string clientId,
            string tenantId,
            string refreshTokenSecretName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var refreshToken = GetRefreshToken(refreshTokenSecretName);
                var authProvider = new RefreshTokenAuthenticationProvider(clientId, GetClientSecret(clientId), refreshToken, _httpClient);
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

        private string GetClientSecret(string clientId)
        {
            // For personal accounts, all sources use the same client ID and secret
            // Try source1-client-secret first (they're all the same)
            if (_secretClient != null)
            {
                try
                {
                    var secret = _secretClient.GetSecret("source1-client-secret");
                    return secret.Value.Value;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to retrieve client secret from Key Vault: {ex.Message}", ex);
                }
            }

            throw new InvalidOperationException("Key Vault is not configured. Cannot retrieve client secret.");
        }
    }
}
