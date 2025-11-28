using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using System.Text.Json;

namespace EprPrnIntegration.Common.Helpers;

public interface IBlobStorage
{
    Task<T?> ReadJsonFromBlob<T>(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task WriteJsonToBlob<T>(string containerName, string blobName, T data, CancellationToken cancellationToken = default);
}

[ExcludeFromCodeCoverage] // This will have coverage at the integration-test level.
public class BlobStorage : IBlobStorage
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorage(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<T?> ReadJsonFromBlob<T>(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);


        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return default;
        }

        var response = await blobClient.DownloadContentAsync(cancellationToken);
        var content = response.Value.Content.ToString();

        return JsonSerializer.Deserialize<T>(content);
    }

    public async Task WriteJsonToBlob<T>(string containerName, string blobName, T data, CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        var json = JsonSerializer.Serialize(data);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
    }
}
