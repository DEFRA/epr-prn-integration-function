namespace EprPrnIntegration.Common.Service;

public interface ILastUpdateService
{
    Task<DateTime?> GetLastUpdate(string name);
    Task SetLastUpdate(string name, DateTime lastUpdate);
}
