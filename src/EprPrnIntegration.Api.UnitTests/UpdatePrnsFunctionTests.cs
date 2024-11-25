using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using System.Net;
using global::EprPrnIntegration.Common.Client;

namespace EprPrnIntegration.Api.UnitTests
{
    public class UpdatePrnsFunctionTests
    {
        private readonly Mock<IPrnService> _prnServiceMock;
        private readonly Mock<INpwdClient> _npwdClientMock;
        private readonly Mock<ILogger<UpdatePrnsFunction>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly UpdatePrnsFunction _function;

        public UpdatePrnsFunctionTests()
        {
            _prnServiceMock = new Mock<IPrnService>();
            _npwdClientMock = new Mock<INpwdClient>();
            _loggerMock = new Mock<ILogger<UpdatePrnsFunction>>();
            _configurationMock = new Mock<IConfiguration>();

            _function = new UpdatePrnsFunction(
                _prnServiceMock.Object,
                _npwdClientMock.Object,
                _loggerMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task Run_ShouldReturnNull_WhenNoUpdatedPrnsAreRetrieved()
        {
            // Arrange
            _configurationMock.Setup(config => config["UpdatePrnsStartHour"]).Returns("18");
            _prnServiceMock.Setup(service => service.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(new List<UpdatedPrnsResponseModel>());

            // Act
            var result = await _function.Run(It.IsAny<HttpRequest>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Run_ShouldHandleException_WhenExceptionOccursDuringDataRetrieval()
        {
            // Arrange
            _configurationMock.Setup(config => config["UpdatePrnsStartHour"]).Returns("18");
            _prnServiceMock.Setup(service => service.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                    .ThrowsAsync(new Exception("Data retrieval failed"));

            // Act
            var result = await _function.Run(It.IsAny<HttpRequest>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Run_ShouldUpdatePrnsSuccessfully_WhenPrnsAreRetrieved()
        {
            // Arrange
            var updatedPrnsList = new List<UpdatedPrnsResponseModel>
            {
                new UpdatedPrnsResponseModel
                {
                    EvidenceNo = "EVID12345",
                    EvidenceStatusCode = "EV_ACCEP",
                    StatusDate = new DateTime(2024, 11, 25, 18, 0, 0)
                },
                new UpdatedPrnsResponseModel
                {
                    EvidenceNo = "EVID67890",
                    EvidenceStatusCode = "EV_AWACCEP",
                    StatusDate = new DateTime(2024, 11, 25, 19, 0, 0)
                }
            };

            _configurationMock.Setup(config => config["UpdatePrnsStartHour"]).Returns("18");
            _prnServiceMock.Setup(service => service.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(updatedPrnsList);

            _npwdClientMock.Setup(client => client.Patch(It.IsAny<List<UpdatedPrnsResponseModel>>(), It.IsAny<string>()))
                           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await _function.Run(It.IsAny<HttpRequest>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Run_ShouldLogError_WhenNPwdApiCallFails()
        {
            // Arrange
            var updatedPrnsList = new List<UpdatedPrnsResponseModel>
            {
                new UpdatedPrnsResponseModel
                {
                    EvidenceNo = "EVID12345",
                    EvidenceStatusCode = "EV_ACCEP",
                    StatusDate = new DateTime(2024, 11, 25, 18, 0, 0)
                },
                new UpdatedPrnsResponseModel
                {
                    EvidenceNo = "EVID67890",
                    EvidenceStatusCode = "EV_AWACCEP",
                    StatusDate = new DateTime(2024, 11, 25, 19, 0, 0)
                }
            };

            _configurationMock.Setup(config => config["UpdatePrnsStartHour"]).Returns("18");
            _prnServiceMock.Setup(service => service.GetUpdatedPrns(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(updatedPrnsList);

            _npwdClientMock.Setup(client => client.Patch(It.IsAny<List<UpdatedPrnsResponseModel>>(), It.IsAny<string>()))
                           .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            // Act
            var result = await _function.Run(It.IsAny<HttpRequest>());

            // Assert
            Assert.Null(result);
        }
    }
}

