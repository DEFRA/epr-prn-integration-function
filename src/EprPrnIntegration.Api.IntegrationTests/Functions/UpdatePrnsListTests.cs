using System.Text.Json;
using AutoFixture;
using EprPrnIntegration.Common.Configuration;
using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Models;
using FluentAssertions;
using Xunit;

namespace EprPrnIntegration.Api.IntegrationTests.Functions;

public class UpdateRrepwPrnsListTests : IntegrationTestBase
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsAcceptedPrnToRrepw()
    {
        var payload = _fixture
            .Build<PrnUpdateStatus>()
            .With(p => p.PrnStatusId, (int)EprnStatus.ACCEPTED)
            .CreateMany()
            .ToList();
        // Arrange: Set up the PRN backend to return an accepted PRN
        // PrnStatusId = 1 corresponds to ACCEPTED status
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnAccept();

        // Act: Invoke the function
        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrnsList);

        // Assert: Verify the accept request was sent to RREPW
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await RrepwApiStub.GetPrnAcceptRequests();

            foreach (var prnUpdate in payload)
            {
                var request = requests.FirstOrDefault(r =>
                    r.Request.Path.Contains(prnUpdate.PrnNumber)
                );
                request.Should().NotBeNull();
                var jsonDocument = JsonDocument.Parse(request.Request.Body!);

                jsonDocument
                    .RootElement.GetProperty("acceptedAt")
                    .GetDateTime()
                    .Should()
                    .Be(prnUpdate.StatusDate);

                request.Response.StatusCode.Should().Be(200);
            }
        });
    }

    [Fact]
    public async Task WhenAzureFunctionIsInvoked_SendsRejectedPrnToRrepw()
    {
        // Arrange: Set up the PRN backend to return a rejected PRN
        // PrnStatusId = 2 corresponds to REJECTED status
        var payload = _fixture
            .Build<PrnUpdateStatus>()
            .With(p => p.PrnStatusId, (int)EprnStatus.REJECTED)
            .CreateMany()
            .ToList();
        await PrnApiStub.HasUpdatedPrns(payload);
        await RrepwApiStub.AcceptsPrnReject();

        // Act: Invoke the function
        await AzureFunctionInvokerContext.InvokeAzureFunction(FunctionName.UpdateRrepwPrnsList);

        // Assert: Verify the reject request was sent to RREPW
        await AsyncWaiter.WaitForAsync(async () =>
        {
            var requests = await RrepwApiStub.GetPrnRejectRequests();

            foreach (var prnUpdate in payload)
            {
                var request = requests.FirstOrDefault(r =>
                    r.Request.Path.Contains(prnUpdate.PrnNumber)
                );
                request.Should().NotBeNull();
                var jsonDocument = JsonDocument.Parse(request.Request.Body!);

                jsonDocument
                    .RootElement.GetProperty("rejectedAt")
                    .GetDateTime()
                    .Should()
                    .Be(prnUpdate.StatusDate);

                request.Response.StatusCode.Should().Be(200);
            }
        });
    }
}
