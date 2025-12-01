using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EprPrnIntegration.Common.Helpers;

namespace EprPrnIntegration.Common.Service;

[ExcludeFromCodeCoverage]
public class LastUpdateService(IBlobStorage blobStorage) : ILastUpdateService
{
    private const string ContainerName = "last-update-store";

    public async Task<DateTime?> GetLastUpdate(string functionName)
    {
        var blobName = $"{functionName}.txt";
        var content = await blobStorage.ReadTextFromBlob(ContainerName, blobName);

        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        return DateTime.ParseExact(content, "O", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }

    public async Task SetLastUpdate(string name, DateTime lastUpdate)
    {
        var blobName = $"{name}.txt";
        var content = lastUpdate.ToString("O"); // ISO 8601 format

        await blobStorage.WriteTextToBlob(ContainerName, blobName, content);
    }
}
