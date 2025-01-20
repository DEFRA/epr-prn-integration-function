namespace EprPrnIntegration.Common.Models;
public class ErrorEvent
{
    public string PrnNumber { get; set; } = default!;
    public string IncomingStatus { get; set; } = default!;  
    public string Date { get; set; } = default!;
    public string OrganisationName { get; set; } = default!;
    public string ErrorComments { get; set; } = default!;
}