using System.Text;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Client;

public class NpwdClient(
    IHttpClientFactory httpClientFactory,
    IConfigurationService configurationService,
    ILogger<NpwdClient> logger) : INpwdClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Constants.HttpClientNames.Npwd);

    public async Task<HttpResponseMessage> Patch<T>(T dataModel, string path)
    {
        var data = JsonConvert.SerializeObject(dataModel);
        var requestContent = new StringContent(data, Encoding.UTF8, "application/json");

        var baseAddress = configurationService.GetNpwdApiBaseUrl();
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new UriFormatException("Base address for NPWD API is null or empty.");
        }

        _httpClient.BaseAddress = new Uri(baseAddress!);

        var response = await _httpClient.PatchAsync(path, requestContent);
        return response;
    }

    public async Task<List<NpwdPrn>> GetIssuedPrns(string filter)
    {
        logger.LogInformation("Fetching prns from npwd with filter {filter}", filter);

        var baseAddress = configurationService.GetNpwdApiBaseUrl();
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new UriFormatException("Base address for NPWD API is null or empty.");
        }

        _httpClient.BaseAddress = new Uri(baseAddress!);

        var response = await _httpClient.GetAsync($"oData/PRNs?$filter={filter}");
        response.EnsureSuccessStatusCode();
        var jsonBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetPrnsResponseModel>(jsonBody)!;

        logger.LogInformation("Fetched total: {totalPrns} prns from npwd", result.Value.Count);
        return result.Value;
    }
}