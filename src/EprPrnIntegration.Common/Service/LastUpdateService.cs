using System.Diagnostics.CodeAnalysis;
using EprPrnIntegration.Common.Helpers;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public class LastUpdateService : ILastUpdateService
{
    private readonly IBlobStorage _blobStorage;
    private const string ContainerName = "last-update-store";

    public LastUpdateService(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public async Task<DateTime?> GetLastUpdate(string name)
    {
        var blobName = $"{name}.json";
        var data = await _blobStorage.ReadJsonFromBlob<LastUpdateData>(ContainerName, blobName);

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

        await _blobStorage.WriteJsonToBlob(ContainerName, blobName, data);
    }

    private class LastUpdateData
    {
        public DateTime LastUpdate { get; set; }
    }
}
