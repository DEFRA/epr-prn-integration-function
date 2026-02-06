using Azure.Storage.Blobs;
using EprPrnIntegration.Api.Middleware;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Service;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class FunctionExecutionContext
{
    public static readonly ILastUpdateService LastUpdateService;

    private static readonly BlobStorage BlobStorage;

    static FunctionExecutionContext()
    {
        // This connection string is the well-known default credential for the Azurite storage emulator.
        // It is NOT a sensitive secret - it's a hardcoded value built into Azurite for local development.
        // See: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite#well-known-storage-account-and-key
        var connectionString =
            "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;";
        var blobServiceClient = new BlobServiceClient(connectionString);
        
        BlobStorage = new BlobStorage(blobServiceClient);
        LastUpdateService = new LastUpdateService(BlobStorage);
    }
    
    public static async Task<bool> IsRunning(string functionName)
    {
        var blobName = $"{functionName}.txt";
        var content = await BlobStorage.ReadTextFromBlob(FunctionRunningMiddleware.ContainerName, blobName);

        return !string.IsNullOrEmpty(content) && bool.Parse(content);
    }
}
