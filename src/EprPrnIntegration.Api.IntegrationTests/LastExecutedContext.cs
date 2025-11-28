using Azure.Storage.Blobs;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Service;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class LastExecutedContext
{
    private static readonly BlobServiceClient BlobServiceClient;
    private static readonly IBlobStorage BlobStorage;
    public static readonly ILastUpdateService LastUpdateService;

    static LastExecutedContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        BlobServiceClient = new BlobServiceClient(connectionString);
        BlobStorage = new BlobStorage(BlobServiceClient);
        LastUpdateService = new LastUpdateService(BlobStorage);
    }
}
