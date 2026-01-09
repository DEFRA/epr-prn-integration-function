using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Common.RESTServices.WasteOrganisationsService.Interfaces
{
    public interface IWasteOrganisationsService
    {
        Task<HttpResponseMessage> UpdateOrganisation(
            string id,
            WasteOrganisationsApiUpdateRequest organisation
        );
    }
}
