namespace EprPrnIntegration.Common.Models.Npwd;

public class ReconcileIssuedPrn
{
    public string PrnNumber { get; set; } = default!;

    public string PrnStatus { get; set; } = default!;

    public string UploadedDate { get; set; } = default!;

    public string OrganisationName { get; set; } = default!;
}
