using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace EprPrnIntegration.Api.Middleware;

[ExcludeFromCodeCoverage]
public class FunctionRunningMiddleware(
    IBlobStorage blobStorage, 
    ILogger<FunctionRunningMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public const string ContainerName = "running-store";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            logger.LogInformation("Before function execution: {Name}", context.FunctionDefinition.Name);

            await next(context);
        }
        finally
        {
            logger.LogInformation("After function execution: {Name}", context.FunctionDefinition.Name);

            await SetLastRun(context.FunctionDefinition.Name);
        }
    }

    private async Task SetLastRun(string functionName)
    {
        var blobName = $"{functionName}.txt";
        var content = DateTime.UtcNow.ToString("O"); // ISO 8601 format

        await blobStorage.WriteTextToBlob(ContainerName, blobName, content);
    }
}