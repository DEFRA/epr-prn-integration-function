using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Helpers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace EprPrnIntegration.Api.Middleware;

[ExcludeFromCodeCoverage]
public class FunctionRunningMiddleware(IBlobStorage blobStorage) : IFunctionsWorkerMiddleware
{
    public const string ContainerName = "running-store";
    
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            Console.WriteLine($"Before function execution: {context.FunctionDefinition.Name}");
            
            await SetIsRunning(context.FunctionDefinition.Name, true);

            await next(context);
        }
        finally
        {
            Console.WriteLine($"After function execution: {context.FunctionDefinition.Name}");
            
            await SetIsRunning(context.FunctionDefinition.Name, false);
        }
    }

    private async Task SetIsRunning(string functionName, bool isRunning)
    {
        var blobName = $"{functionName}.txt";

        await blobStorage.WriteTextToBlob(ContainerName, blobName, isRunning.ToString().ToLower());
    }
}