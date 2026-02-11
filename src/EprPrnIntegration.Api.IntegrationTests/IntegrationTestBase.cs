using EprPrnIntegration.Api.IntegrationTests.Stubs;
using EprPrnIntegration.Common.Models.Rrepw;
using EprPrnIntegration.Common.Models.WasteOrganisationsApi;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests;

[Trait("Category", "IntegrationTest")]
[Collection("Integration Tests")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected AccountApi AccountApiStub = null!;
    protected CommonDataApi CommonDataApiStub = null!;
    protected NpwdApi NpwdApiStub = null!;
    protected PrnApi PrnApiStub = null!;
    protected WasteOrganisationsApi WasteOrganisationsApiStub = null!;
    protected CognitoApi CognitoApiStub = null!;
    protected RrepwApi RrepwApiStub = null!;

    private WireMockContext _wireMockContext = null!;

    public async Task InitializeAsync()
    {
        _wireMockContext = new WireMockContext();

        await _wireMockContext.InitializeAsync();

        NpwdApiStub = new NpwdApi(_wireMockContext);
        CommonDataApiStub = new CommonDataApi(_wireMockContext);
        PrnApiStub = new PrnApi(_wireMockContext);
        AccountApiStub = new AccountApi(_wireMockContext);
        WasteOrganisationsApiStub = new WasteOrganisationsApi(_wireMockContext);
        CognitoApiStub = new CognitoApi(_wireMockContext);
        RrepwApiStub = new RrepwApi(_wireMockContext);
    }

    public async Task DisposeAsync()
    {
        await _wireMockContext.DisposeAsync();
    }

    protected static async Task LastUpdateShouldHaveChanged(DateTime before, string functionName)
    {
        var after = await FunctionContext.GetLastUpdate(functionName) ?? DateTime.MinValue;
        after.Should().BeAfter(before);
    }

    protected static async Task LastUpdateShouldNotHaveChanged(DateTime before, string functionName)
    {
        var after = await FunctionContext.GetLastUpdate(functionName) ?? DateTime.MinValue;
        after.Should().NotBeAfter(before);
    }

    protected async Task SetupOrganisations(List<PackagingRecyclingNote> prns)
    {
        await CognitoApiStub.SetupOAuthToken();

        foreach (var prn in prns)
        {
            await WasteOrganisationsApiStub.WithOrganisation(
                Guid.Parse(prn.IssuedToOrganisation!.Id!),
                WoApiOrganisationType.ComplianceScheme
            );
        }
    }
}
