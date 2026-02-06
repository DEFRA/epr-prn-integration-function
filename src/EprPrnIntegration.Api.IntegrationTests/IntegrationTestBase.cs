using EprPrnIntegration.Api.IntegrationTests.Stubs;
using EprPrnIntegration.Common.Service;
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
    private WireMockContext WireMockContext = null!;

    public async Task InitializeAsync()
    {
        WireMockContext = new WireMockContext();

        await WireMockContext.InitializeAsync();

        NpwdApiStub = new NpwdApi(WireMockContext);
        CommonDataApiStub = new CommonDataApi(WireMockContext);
        PrnApiStub = new PrnApi(WireMockContext);
        AccountApiStub = new AccountApi(WireMockContext);
        WasteOrganisationsApiStub = new WasteOrganisationsApi(WireMockContext);
        CognitoApiStub = new CognitoApi(WireMockContext);
        RrepwApiStub = new RrepwApi(WireMockContext);
    }

    public async Task DisposeAsync()
    {
        await WireMockContext.DisposeAsync();
    }

    protected static async Task<DateTime> GetLastUpdate(string functionName)
    {
        return await LastExecutedContext.LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
    }

    protected static async Task LastUpdateShouldHaveChanged(DateTime before, string functionName)
    {
        var after = await LastExecutedContext.LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
        after.Should().BeAfter(before);
    }

    protected static async Task LastUpdateShouldNotHaveChanged(DateTime before, string functionName)
    {
        var after = await LastExecutedContext.LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
        after.Should().NotBeAfter(before);
    }
}
