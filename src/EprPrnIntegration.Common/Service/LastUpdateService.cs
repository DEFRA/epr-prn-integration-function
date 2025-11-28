using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Helpers;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public class LastUpdateService(IBlobStorage blobStorage) : ILastUpdateService
{
    private const string ContainerName = "last-update-store";

    public async Task<DateTime?> GetLastUpdate(string name)
    {
        var blobName = $"{name}.json";
        var data = await blobStorage.ReadJsonFromBlob<LastUpdateData>(ContainerName, blobName);

        if (data == null)
        {
            return null;
        }

        return data.LastUpdate;
    }

    public async Task SetLastUpdate(string name, DateTime lastUpdate)
    {
        var blobName = $"{name}.json";
        var data = new LastUpdateData
        {
            LastUpdate = lastUpdate
        };

        await blobStorage.WriteJsonToBlob(ContainerName, blobName, data);
    }

    private sealed class LastUpdateData
    {
        public DateTime LastUpdate { get; set; }
    }
}
