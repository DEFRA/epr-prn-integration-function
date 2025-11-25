using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Models.Rrepw;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EprPrnIntegration.Common.Client;

// A temporary stub for RREPW's producer endpoint
[ExcludeFromCodeCoverage]
public class RrepwClient(
    ILogger<RrepwClient> logger)
{
    public Task Patch(ProducerUpdateRequest request)
    {
        var json = JsonConvert.SerializeObject(new {
           id = request.Id,
           status = request.Status,
           type = request.Type,
        });
        
        logger.LogInformation("Patching producer update to RREPW: ${json}", json);
        
        return Task.CompletedTask;
    } 
}