namespace EprPrnIntegration.Common.Service;

public interface ILastUpdateService
{
    Task<DateTime?> GetLastUpdate(string functionName);
    Task SetLastUpdate(string name, DateTime lastUpdate);
}
