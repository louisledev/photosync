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

        public GraphServiceClient CreateClient(string clientId, string tenantId, string refreshTokenSecretName, string? clientSecretName = null)
        {
            // Always use refresh token authentication for personal Microsoft accounts
            var refreshToken = GetRefreshToken(refreshTokenSecretName);
            var authProvider = new RefreshTokenAuthenticationProvider(clientId, GetClientSecret(clientSecretName), refreshToken, _httpClient);
            return new GraphServiceClient(authProvider);
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateRefreshTokenAsync(
            string clientId,
            string tenantId,
            string refreshTokenSecretName,
            string? clientSecretName = null,
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

        private string GetClientSecret(string? clientSecretName = null)
        {
            if (_secretClient == null)
            {
                throw new InvalidOperationException("Key Vault is not configured. Cannot retrieve client secret.");
            }

            // Determine the secret name with fallback priority:
            // 1. Explicitly provided clientSecretName parameter (highest priority)
            // 2. KeyVault:ClientSecretName from application configuration
            // 3. Default "source1-client-secret" for backward compatibility
            var secretName = clientSecretName 
                ?? _configuration["KeyVault:ClientSecretName"] 
                ?? "source1-client-secret";

            try
            {
                var secret = _secretClient.GetSecret(secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve client secret '{secretName}' from Key Vault: {ex.Message}", ex);
            }
        }
    }
}
