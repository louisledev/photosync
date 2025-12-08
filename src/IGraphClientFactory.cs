using Microsoft.Graph;

namespace PhotoSync
{
    /// <summary>
    /// Factory interface for creating Microsoft Graph clients
    /// </summary>
    public interface IGraphClientFactory
    {
        /// <summary>
        /// Create a GraphServiceClient with the specified credentials
        /// </summary>
        /// <param name="clientId">Azure AD application client ID</param>
        /// <param name="refreshTokenSecretName">Name of the Key Vault secret containing the refresh token</param>
        /// <param name="clientSecretName">Name of the Key Vault secret containing the client secret. If not provided, uses configuration or default.</param>
        /// <returns>Configured GraphServiceClient</returns>
        GraphServiceClient CreateClient(string clientId, string refreshTokenSecretName, string clientSecretName);

        /// <summary>
        /// Validates that a refresh token is still valid
        /// </summary>
        /// <param name="clientId">Azure AD application client ID</param>
        /// <param name="refreshTokenSecretName">Name of the Key Vault secret containing the refresh token</param>
        /// <param name="clientSecretName">Name of the Key Vault secret containing the client secret. If not provided, uses configuration or default.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple of (IsValid, ErrorMessage)</returns>
        Task<(bool IsValid, string? ErrorMessage)> ValidateRefreshTokenAsync(
            string clientId,
            string refreshTokenSecretName,
            string clientSecretName,
            CancellationToken cancellationToken = default);
    }
}
