using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;

namespace EprPrnIntegration.Common.Helpers;

public interface IBlobStorage
{
    Task<string?> ReadTextFromBlob(string containerName, string blobName);
    Task WriteTextToBlob(string containerName, string blobName, string content);
}

[ExcludeFromCodeCoverage] // This will have coverage at the integration-test level.
public class BlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorage(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string?> ReadTextFromBlob(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }

    public async Task WriteTextToBlob(string containerName, string blobName, string content)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
