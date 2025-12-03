using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Client;

// A temporary stub for the waste-organisations-api endpoint
[ExcludeFromCodeCoverage]
public class WasteOrganisationsApiClient(
    ILogger<WasteOrganisationsApiClient> logger)
{
    public Task Patch(WasteOrganisationsApiUpdateRequest request)
    {
        var json = JsonConvert.SerializeObject(new {
           registration = new
           {
               status = request.Registration.Status,
               type = request.Registration.Type,
           },
           businessCountry = request.BusinessCountry,
        });
        
        logger.LogInformation("Patching producer update to RREPW: ${json}", json);
        
        return Task.CompletedTask;
    } 
}