using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.Middleware;

[ExcludeFromCodeCoverage]
public class WasteOrganisationsApiAuthorisationHandler : DelegatingHandler
{
    private readonly WasteOrganisationsApiConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public WasteOrganisationsApiAuthorisationHandler(
        IOptions<WasteOrganisationsApiConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var token = await GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Check if we have a valid cached token
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            // Get new token from Cognito
            var token = await FetchCognitoTokenAsync(cancellationToken);
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> FetchCognitoTokenAsync(CancellationToken cancellationToken)
    {
        var clientCredentials = $"{_config.ClientId}:{_config.ClientSecret}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientCredentials));

        using var httpClient = _httpClientFactory.CreateClient();

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _config.AccessTokenUrl);
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _config.ClientId },
            { "client_secret", _config.ClientSecret }
        };

        tokenRequest.Content = new FormUrlEncodedContent(formData);

        var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<CognitoTokenResponse>(responseContent);

        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Failed to retrieve access token from Cognito");
        }

        _cachedToken = tokenResponse.AccessToken;

        // Set token expiry with a 5-minute buffer before actual expiration
        var expiresIn = tokenResponse.ExpiresIn > 300 ? tokenResponse.ExpiresIn - 300 : tokenResponse.ExpiresIn;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        return _cachedToken;
    }

    private class CognitoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
