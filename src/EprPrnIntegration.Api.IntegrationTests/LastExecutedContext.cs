using Azure.Storage.Blobs;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Service;

namespace EprPrnIntegration.Api.IntegrationTests;

public static class LastExecutedContext
{
    public static readonly ILastUpdateService LastUpdateService;

    static LastExecutedContext()
    {
        var connectionString = DockerEnvironmentHelper.GetAzureWebJobsStorage();
        var blobServiceClient = new BlobServiceClient(connectionString);
        var blobStorage = new BlobStorage(blobServiceClient);
        LastUpdateService = new LastUpdateService(blobStorage);
    }
}
