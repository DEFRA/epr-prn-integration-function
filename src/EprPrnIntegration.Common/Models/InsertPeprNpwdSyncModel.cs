namespace EprPrnIntegration.Common.Models
{
    public class InsertPeprNpwdSyncModel
    {
        public required string EvidenceNo { get; set; }
        public required string EvidenceStatus { get; set; }


        public static explicit operator InsertPeprNpwdSyncModel(UpdatedPrnsResponseModel syncedPrns)
        {
            return new InsertPeprNpwdSyncModel()
            {
                EvidenceNo = syncedPrns.EvidenceNo,
                EvidenceStatus = MapToPeprStatus(syncedPrns.EvidenceStatusCode)
            };
        }

        private static string MapToPeprStatus(string evidenceStatusCode)
        {
            return evidenceStatusCode switch
            {
                "EV-ACCEP" => "ACCEPTED",
                "EV-ACANCEL" => "REJECTED",
                _ => throw new InvalidDataException($"Unexpected Status sent to npwd {evidenceStatusCode}")
            };
        }
    }
}
