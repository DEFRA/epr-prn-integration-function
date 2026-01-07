using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EprPrnIntegration.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EprPrnIntegration.Common.RESTServices
{
    /// <summary>
    /// Don't use this without. BaseHttpService is the place to be.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class BaseHttpServiceOld
    {
        protected readonly string _baseUrl;
        protected readonly HttpClient _httpClient;
        protected IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BaseHttpServiceOld> _logger;

        protected BaseHttpServiceOld(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            string baseUrl,
            string endPointName,
            ILogger<BaseHttpServiceOld> logger,
            string httpClientName = "",
            int timeoutSeconds = 100
        )
        {
            _httpContextAccessor =
                httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            // Initialize _baseUrl in the constructor
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? throw new ArgumentNullException(nameof(baseUrl))
                : baseUrl;

            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endPointName);

            _httpClient = string.IsNullOrWhiteSpace(httpClientName)
                ? httpClientFactory.CreateClient()
                : httpClientFactory.CreateClient(httpClientName);

            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            _httpClient.DefaultRequestHeaders.Add(
                Constants.HttpHeaderNames.Accept,
                "application/json"
            );

            if (_baseUrl.EndsWith('/'))
                _baseUrl = _baseUrl.TrimEnd('/');

            _baseUrl = $"{_baseUrl}/{endPointName}";
            _logger = logger;
        }

        protected void SetBearerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
            );
        }

        /// <summary>
        /// Performs an Http GET returning the specified object
        /// </summary>
        protected virtual async Task<T> Get<T>(
            string url,
            CancellationToken cancellationToken,
            bool includeTrailingSlash = true
        )
        {
            if (string.IsNullOrEmpty(url))
            {
                url = _baseUrl;
            }
            else
            {
                if (includeTrailingSlash)
                {
                    url = $"{_baseUrl}/{url}/";
                }
                else
                {
                    url = $"{_baseUrl}/{url}";
                }
            }

            return await Send<T>(CreateMessage(url, null, HttpMethod.Get), cancellationToken);
        }

        protected virtual async Task<bool> GetOk(
            string url,
            CancellationToken cancellationToken,
            bool includeTrailingSlash = true
        )
        {
            if (string.IsNullOrEmpty(url))
            {
                url = _baseUrl;
            }
            else
            {
                if (includeTrailingSlash)
                {
                    url = $"{_baseUrl}/{url}/";
                }
                else
                {
                    url = $"{_baseUrl}/{url}";
                }
            }

            var requestMessage = CreateMessage(url, null, HttpMethod.Get);
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }

        private string ReturnUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            return $"{_baseUrl}/{url}/";
        }

        /// <summary>
        /// Performs an Http POST returning the specified object
        /// </summary>
        protected virtual async Task<T> Post<T>(
            string url,
            object? payload,
            CancellationToken cancellationToken
        )
        {
            url = ReturnUrl(url);
            return await Send<T>(CreateMessage(url, payload, HttpMethod.Post), cancellationToken);
        }

        /// <summary>
        /// Performs an Http POST without returning any data
        /// </summary>
        protected async Task Post(string url, object? payload, CancellationToken cancellationToken)
        {
            url = ReturnUrl(url);
            await Send(CreateMessage(url, payload, HttpMethod.Post), cancellationToken);
        }

        /// <summary>
        /// Performs an Http PUT returning the specified object
        /// </summary>
        protected async Task<T> Put<T>(
            string url,
            object? payload,
            CancellationToken cancellationToken
        )
        {
            url = ReturnUrl(url);
            return await Send<T>(CreateMessage(url, payload, HttpMethod.Put), cancellationToken);
        }

        /// <summary>
        /// Performs an Http PUT without returning any data
        /// </summary>
        protected async Task Put(string url, object? payload, CancellationToken cancellationToken)
        {
            url = ReturnUrl(url);
            await Send(CreateMessage(url, payload, HttpMethod.Put), cancellationToken);
        }

        /// <summary>
        /// Performs an Http DELETE returning the specified object
        /// </summary>
        protected async Task<T> Delete<T>(
            string url,
            object? payload,
            CancellationToken cancellationToken
        )
        {
            url = ReturnUrl(url);
            return await Send<T>(CreateMessage(url, payload, HttpMethod.Delete), cancellationToken);
        }

        /// <summary>
        /// Performs an Http DELETE without returning any data
        /// </summary>
        protected async Task Delete(
            string url,
            object? payload,
            CancellationToken cancellationToken
        )
        {
            url = ReturnUrl(url);
            await Send(CreateMessage(url, payload, HttpMethod.Delete), cancellationToken);
        }

        private static HttpRequestMessage CreateMessage(
            string url,
            object? payload,
            HttpMethod httpMethod
        )
        {
            var msg = new HttpRequestMessage { RequestUri = new Uri(url), Method = httpMethod };

            if (payload != null)
            {
                msg.Content = JsonContent.Create(payload);
            }

            return msg;
        }

        private async Task<T> Send<T>(
            HttpRequestMessage requestMessage,
            CancellationToken cancellationToken
        )
        {
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var streamReader = new StreamReader(responseStream);
                var content = await streamReader.ReadToEndAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(content))
                    return default!;

                return ReturnValue<T>(content);
            }
            else
            {
                // get any message from the response
                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var content = default(string);

                if (responseStream.Length > 0)
                {
                    using var streamReader = new StreamReader(responseStream);
                    content = await streamReader.ReadToEndAsync(cancellationToken);
                }

                // set the response status code and throw the exception for the middleware to handle
                throw new ResponseCodeException(response.StatusCode, content!);
            }
        }

        private async Task Send(
            HttpRequestMessage requestMessage,
            CancellationToken cancellationToken
        )
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP request failed {BaseAddress}. Exception: {Message}",
                    _httpClient.BaseAddress,
                    ex.Message
                );
                throw new ServiceException(
                    $"Error occurred while sending HTTP request  {_httpClient.BaseAddress}.",
                    ex.StatusCode,
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error occurred during HTTP request {BaseAddress}.",
                    _httpClient.BaseAddress
                );
                throw new ServiceException(
                    $"Unexpected error occurred while processing the HTTP request  {_httpClient.BaseAddress}.",
                    null,
                    ex
                );
            }
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "API call failed. Status Code: {StatusCode}. Response Body: {ResponseBody}",
                    response.StatusCode,
                    responseBody
                );

                throw new ServiceException(
                    $"Error occurred calling API {_httpClient.BaseAddress} with error code: {response.StatusCode}. "
                        + $"Message: {responseBody}",
                    response.StatusCode
                );
            }
        }

        private static T ReturnValue<T>(string value)
        {
            if (IsValidJson(value))
                return JsonConvert.DeserializeObject<T>(value)!;
            else
                return (T)Convert.ChangeType(value, typeof(T));
        }

        private static bool IsValidJson(string stringValue)
        {
            try
            {
                JToken.Parse(stringValue);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
