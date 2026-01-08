using EprPrnIntegration.Common.Enums;

namespace EprPrnIntegration.Common.Mappers;

public static class NpwdStatusToPrnStatusMapper
{
    public static EprnStatus? Map(string npwdStatus)
    {
        if (string.IsNullOrEmpty(npwdStatus))
        {
            return null;
        }

        switch (npwdStatus.ToUpper())
        {
            case "EV-ACCEP":
                return EprnStatus.ACCEPTED;
            case "EV-ACANCEL":
                return EprnStatus.REJECTED;
            case "EV-CANCEL":
                return EprnStatus.CANCELLED;
            case "EV-AWACCEP":
            case "EV-AWACCEP-EPR":
                return EprnStatus.AWAITINGACCEPTANCE;
            default:
                return null;
        }
    }
}
