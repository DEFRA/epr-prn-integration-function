using System.Text;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Client;

public class NpwdClient(
    IHttpClientFactory httpClientFactory,
    IOptions<NpwdIntegrationConfiguration> npwdIntegrationConfig,
    ILogger<NpwdClient> logger) : INpwdClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Constants.HttpClientNames.Npwd);

    public async Task<HttpResponseMessage> Patch<T>(T updatedProducers, string updatePath)
    {
        var data = JsonConvert.SerializeObject(updatedProducers);
        var requestContent = new StringContent(data, Encoding.UTF8, "application/json");

        var baseAddress = npwdIntegrationConfig.Value.BaseUrl;
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new UriFormatException("Base address for NPWD API is null or empty.");
        }

        _httpClient.BaseAddress = new Uri(baseAddress!);

        var response = await _httpClient.PatchAsync(updatePath, requestContent);
        return response;
    }

    public async Task<List<NpwdPrn>> GetIssuedPrns(string filter)
    {
        logger.LogInformation("Fetching prns from npwd with filter {filter}", filter);

        var baseAddress = npwdIntegrationConfig.Value.BaseUrl;
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new UriFormatException("Base address for NPWD API is null or empty.");
        }

        if (baseAddress.EndsWith('/'))
            baseAddress = baseAddress.TrimEnd('/');

        var requestUrl = $"{baseAddress}/{Constants.NpwdApiPath.Prns}?$filter={filter}";
        List<NpwdPrn> issuedPrns = [];
        while(!string.IsNullOrEmpty(requestUrl))
        {
            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();
            var jsonBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GetPrnsResponseModel>(jsonBody)!;
            issuedPrns.AddRange(result.Value);
            requestUrl = result.NextLink;
        }

        logger.LogInformation("Fetched total: {totalPrns} prns from npwd", issuedPrns.Count);
        return issuedPrns;
    }
}