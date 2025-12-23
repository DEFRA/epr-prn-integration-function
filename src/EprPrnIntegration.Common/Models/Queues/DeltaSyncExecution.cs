namespace EprPrnIntegration.Common.Models.Queues;

public class DeltaSyncExecution
{
    // Synchronisation type
    public NpwdDeltaSyncType SyncType { get; set; }

    // Last run datetime
    public DateTime LastSyncDateTime { get; set; }
}
