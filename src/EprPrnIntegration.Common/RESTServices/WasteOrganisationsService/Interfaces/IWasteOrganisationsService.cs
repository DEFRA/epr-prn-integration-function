namespace EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces
{
    public interface IWasteOrganisationsService
    {
        Task<bool> GetOrganisation(string id);
    }
}
