using EprPrnIntegration.Common.Enums;

namespace EprPrnIntegration.Common.Mappers;

public static class RrepwStatusToPrnStatusMapper
{
    public static EprnStatus? Map(string rrepwStatus)
    {
        if (string.IsNullOrEmpty(rrepwStatus))
        {
            return null;
        }

        switch (rrepwStatus.ToUpper())
        {
            case "ACCEPTED":
                return EprnStatus.ACCEPTED;
            case "REJECTED":
                return EprnStatus.REJECTED;
            case "CANCELLED":
                return EprnStatus.CANCELLED;
            case "AWAITINGACCEPTANCE":
                return EprnStatus.AWAITINGACCEPTANCE;
            case "ACTIVE":
                return EprnStatus.AWAITINGACCEPTANCE; // Mapping ACTIVE to AWAITINGACCEPTANCE for now
            default:
                return null;
        }
    }
}
