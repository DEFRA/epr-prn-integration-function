using EprPrnIntegration.Api.IntegrationTests.Stubs;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;

namespace EprPrnIntegration.Api.IntegrationTests.Functions
{
    public static class TestHelper
    {
        public static async Task SetupOrganisations(
            List<PackagingRecyclingNote> prns,
            CognitoApi cognitoApiStub,
            WasteOrganisationsApi WasteOrganisationsApiStub
        )
        {
            await cognitoApiStub.SetupOAuthToken();
            foreach (var prn in prns)
                await WasteOrganisationsApiStub.WithOrganisation(
                    Guid.Parse(prn.IssuedToOrganisation!.Id!),
                    WoApiOrganisationType.ComplianceScheme
                );
        }
    }
}
