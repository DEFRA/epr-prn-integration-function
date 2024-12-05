using EprPrnIntegration.Common.Models.Queues;

namespace EprPrnIntegration.Common.Helpers;

public interface IUtilities
{
    Task<DeltaSyncExecution> GetDeltaSyncExecution();
    Task SetDeltaSyncExecution(DeltaSyncExecution syncExecution, DateTime latestRun);
}