using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EprPrnIntegration.Common.Configuration;
using Microsoft.Extensions.Options;

namespace EprPrnIntegration.Common.Middleware;

[ExcludeFromCodeCoverage(Justification = "This will have test coverage via integration tests.")]
public class WasteOrganisationsApiAuthorisationHandler(
    IOptions<WasteOrganisationsApiConfiguration> config,
    IHttpClientFactory httpClientFactory)
    : DelegatingHandler
{
    private readonly WasteOrganisationsApiConfiguration _config = config.Value;
    private string? _cachedAccessToken;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var token = await GetCognitoTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(Constants.HttpHeaderNames.Bearer, token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetCognitoTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedAccessToken != null)
        {
            return _cachedAccessToken;
        }

        var clientCredentials = $"{_config.ClientId}:{_config.ClientSecret}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientCredentials));

        using var httpClient = httpClientFactory.CreateClient();

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

        _cachedAccessToken = tokenResponse.AccessToken;
        return _cachedAccessToken;
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
