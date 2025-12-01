using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using System.Text.Json;

namespace EprPrnIntegration.Common.Helpers;

public interface IBlobStorage
{
    Task<T?> ReadJsonFromBlob<T>(string containerName, string blobName);
    Task WriteJsonToBlob<T>(string containerName, string blobName, T data);
}

[ExcludeFromCodeCoverage] // This will have coverage at the integration-test level.
public class BlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorage(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<T?> ReadJsonFromBlob<T>(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);


        if (!await blobClient.ExistsAsync())
        {
            return default;
        }

        var response = await blobClient.DownloadContentAsync();
        var content = response.Value.Content.ToString();

        return JsonSerializer.Deserialize<T>(content);
    }

    public async Task WriteJsonToBlob<T>(string containerName, string blobName, T data)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);

        var json = JsonSerializer.Serialize(data);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        await blobClient.UploadAsync(stream, overwrite: true);
    }
}
