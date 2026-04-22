using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace EprPrnIntegration.Api.Functions;

[ExcludeFromCodeCoverage(Justification = "Covered in integration tests")]
public static class HealthCheckFunction
{
    [Function("HealthCheck")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        var healthStatus = new { Status = "Healthy", Timestamp = DateTime.UtcNow };

        await response.WriteAsJsonAsync(healthStatus);

        return response;
    }
}
