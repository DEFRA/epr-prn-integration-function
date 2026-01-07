using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EprPrnIntegration.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.RESTServices;

/// <summary>
/// A cleaner HTTP service base class that:
/// - Has a single SendAsync method to avoid duplication
/// - Does not catch and wrap exceptions unnecessarily
/// - Lets HttpRequestException (network failures) propagate unchanged
/// - Only throws ServiceException for non-success HTTP status codes
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseHttpService
{
    protected readonly string _baseUrl;
    protected readonly HttpClient _httpClient;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    protected BaseHttpService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        string baseUrl,
        ILogger logger,
        string httpClientName = "",
        int timeoutSeconds = 100
    )
    {
        _httpContextAccessor =
            httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        _httpClient = string.IsNullOrWhiteSpace(httpClientName)
            ? httpClientFactory.CreateClient()
            : httpClientFactory.CreateClient(httpClientName);

        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add(Constants.HttpHeaderNames.Accept, "application/json");

        _baseUrl = baseUrl;
    }

    protected void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token
        );
    }

    #region HTTP Methods

    protected Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    protected Task<T> PostAsync<T>(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync<T>(HttpMethod.Post, path, payload, cancellationToken);

    protected Task PostAsync(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Post, path, payload, cancellationToken);

    protected Task<T> PutAsync<T>(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync<T>(HttpMethod.Put, path, payload, cancellationToken);

    protected Task PutAsync(
        string path,
        object? payload,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Put, path, payload, cancellationToken);

    protected Task<T> DeleteAsync<T>(
        string path,
        object? payload = null,
        CancellationToken cancellationToken = default
    ) => SendAsync<T>(HttpMethod.Delete, path, payload, cancellationToken);

    protected Task DeleteAsync(
        string path,
        object? payload = null,
        CancellationToken cancellationToken = default
    ) => SendAsync(HttpMethod.Delete, path, payload, cancellationToken);

    #endregion

    #region Core Send Methods

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var response = await SendCoreAsync(method, path, payload, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
            return default!;

        return JsonConvert.DeserializeObject<T>(content)!;
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        await SendCoreAsync(method, path, payload, cancellationToken);
    }

    /// <summary>
    /// Core method that sends an HTTP request.
    /// - HttpRequestException (network failures) propagate unchanged
    /// - Non-success status codes throw ServiceException with the status code
    /// </summary>
    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var url = BuildUrl(path);
        using var request = new HttpRequestMessage(method, url);

        if (payload != null)
        {
            request.Content = JsonContent.Create(payload);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "HTTP {Method} to {Url} failed with {StatusCode}: {ResponseBody}",
                method,
                url,
                (int)response.StatusCode,
                responseBody
            );
            throw new ServiceException(
                $"HTTP {method} to {url} failed with {(int)response.StatusCode} {response.StatusCode}: {responseBody}",
                response.StatusCode
            );
        }

        return response;
    }

    private string BuildUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _baseUrl + "/";

        return _baseUrl + "/" + path.Trim('/') + "/";
    }

    #endregion
}
