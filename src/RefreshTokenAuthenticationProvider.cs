using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace PhotoSync
{
    /// <summary>
    /// Authentication provider that uses OAuth refresh tokens to obtain access tokens
    /// for personal Microsoft accounts
    /// </summary>
    public class RefreshTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private readonly HttpClient _httpClient;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        public RefreshTokenAuthenticationProvider(
            string clientId,
            string clientSecret,
            string refreshToken,
            HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("Client secret cannot be null or empty", nameof(clientSecret));
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
        }

        /// <summary>
        /// Validates that the refresh token is still valid by attempting to get an access token
        /// </summary>
        /// <returns>True if token is valid, false otherwise</returns>
        public async Task<(bool IsValid, string? ErrorMessage)> ValidateRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await GetAccessTokenAsync(cancellationToken);
                return (true, null);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return (false, "Refresh token is invalid or expired. Please regenerate the refresh token using tools/get-refresh-token.js");
                }
                return (false, $"Failed to validate refresh token: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to validate refresh token: {ex.Message}");
            }
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            // Return cached token if still valid (with 5 minute buffer)
            if (!string.IsNullOrEmpty(_cachedAccessToken) &&
                DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return _cachedAccessToken;
            }

            // Use semaphore to prevent multiple concurrent refresh requests
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                if (!string.IsNullOrEmpty(_cachedAccessToken) &&
                    DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                {
                    return _cachedAccessToken;
                }

                // Refresh the access token using the refresh token
                var tokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default offline_access")
                });

                var response = await _httpClient.PostAsync(tokenUrl, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Token refresh failed with status {response.StatusCode}: {errorContent}",
                        null,
                        response.StatusCode);
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    throw new InvalidOperationException("Failed to obtain access token from refresh token");
                }

                _cachedAccessToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                return _cachedAccessToken;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private class TokenResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("scope")]
            public string? Scope { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
        }
    }
}
