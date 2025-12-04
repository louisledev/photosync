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
        /// <param name="tenantId">Azure AD tenant ID</param>
        /// <param name="clientSecret">Azure AD application client secret</param>
        /// <returns>Configured GraphServiceClient</returns>
        GraphServiceClient CreateClient(string clientId, string tenantId, string clientSecret);
    }
}
