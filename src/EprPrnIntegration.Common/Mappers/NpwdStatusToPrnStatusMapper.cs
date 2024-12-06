using EprPrnIntegration.Common.Enums;

namespace EprPrnIntegration.Common.Mappers
{
    public static class NpwdStatusToPrnStatusMapper
    {
        public static PrnStatus? Map(string npwdStatus)
        {
            if (string.IsNullOrEmpty(npwdStatus))
            {
                return null;
            }

            switch (npwdStatus.ToUpper())
            {
                case "EV-ACCEP":
                    return PrnStatus.Accepted;
                case "EV-ACANCEL":
                    return PrnStatus.Rejected;
                case "EV-CANCEL":
                    return PrnStatus.Cancelled;
                case "EV-AWACCEP":
                case "EV-AWACCEP-EPR":
                    return PrnStatus.AwaitingAcceptance;
                default:
                    return null;
            }
        }
    }
}
