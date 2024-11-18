using EprPrnIntegration.Common.Models.Npwd;
using EprPrnIntegration.Common.Service;
using Newtonsoft.Json;
using System.Text;

namespace EprPrnIntegration.Common.RESTServices.NpwdService;

public class ProducerService(
    IHttpClientFactory httpClientFactory,
    IConfigurationService configurationService) : IProducerService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(Common.Constants.HttpClientNames.Npwd);

    public async Task<HttpResponseMessage> UpdateProducerList(List<Producer> updatedProducers)
    {
        var producersData = JsonConvert.SerializeObject(updatedProducers);
        var requestContent = new StringContent(producersData, Encoding.UTF8, "application/json");

        var baseAddress = configurationService.GetNpwdApiBaseUrl();
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new UriFormatException("Base address for NPWD API is null or empty.");
        }

        _httpClient.BaseAddress = new Uri(baseAddress!);

        var response = await _httpClient.PostAsync("odata/Producers", requestContent);
        return response;
    }
}