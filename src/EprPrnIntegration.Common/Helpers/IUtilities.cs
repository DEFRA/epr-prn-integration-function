using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Models.Queues;

namespace EprPrnIntegration.Common.Helpers;

public interface IUtilities
{
    Task<DeltaSyncExecution> GetDeltaSyncExecution(NpwdDeltaSyncType syncType);
    Task SetDeltaSyncExecution(DeltaSyncExecution syncExecution, DateTime latestRun);
    void AddCustomEvent(string eventName, IDictionary<string, string> eventData);
    string CreateCsvContent(Dictionary<string, List<string>> data);

    /// <summary>
    /// So happens the NPWD server date time is out of sync with the Epr server date time
    /// Rollback the current date time to avoid skipping over Prns on NPWD
    /// </summary>
    /// <param name="theDate">The date and time to poll up to.</param>
    /// <param name="configSeconds">The lag to offset the polling date.</param>
    /// <returns>Current date and time set back a configurable number of seconds.</returns>
    DateTime OffsetDateTimeWithLag(DateTime theDate, string? configSeconds);
}