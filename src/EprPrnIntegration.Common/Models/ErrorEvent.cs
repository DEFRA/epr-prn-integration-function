namespace EprPrnIntegration.Common.Models;
public class ErrorEvent
{
    public string PrnNumber { get; set; }
    public string IncomingStatus { get; set; }
    public string Date { get; set; }
    public string OrganisationName { get; set; }
    public string ErrorComments { get; set; }
}