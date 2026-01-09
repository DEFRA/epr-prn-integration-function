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
    protected ILastUpdateService LastUpdateService = LastExecutedContext.LastUpdateService;
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

    protected async Task<DateTime> GetLastUpdate(string functionName)
    {
        return await LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
    }

    protected async Task AfterShouldBeAfterBefore(DateTime before, string functionName)
    {
        var after = await LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
        after.Should().BeAfter(before);
    }

    protected async Task AfterShouldNotBeAfterBefore(DateTime before, string functionName)
    {
        var after = await LastUpdateService.GetLastUpdate(functionName) ?? DateTime.MinValue;
        after.Should().NotBeAfter(before);
    }
}
