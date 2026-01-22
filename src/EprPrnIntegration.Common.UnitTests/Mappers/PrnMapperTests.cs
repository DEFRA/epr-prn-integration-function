using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Mappers
{
    public class PrnMapperTests
    {
        private readonly Mock<IConfiguration> _configurationMock;

        public PrnMapperTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock
                .Setup(c => c["PrnsContext"])
                .Returns("https://fat.npwd.org.uk/odata/PRNs/$delta");
        }

        [Fact]
        public void Map_NullInput_ReturnsEmptyPrnDelta()
        {
            // Act
            var result = PrnMapper.Map(null!, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Context);
            Assert.Equal("https://fat.npwd.org.uk/odata/PRNs/$delta", result.Context);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public void Map_EmptyList_ReturnsEmptyPrnDelta()
        {
            // Arrange
            var updatedPrns = new List<UpdatedNpwdPrnsResponseModel>();

            // Act
            var result = PrnMapper.Map(updatedPrns, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://fat.npwd.org.uk/odata/PRNs/$delta", result.Context);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public void Map_ValidInput_MapsToPrnDelta()
        {
            // Arrange
            var updatedPrns = new List<UpdatedNpwdPrnsResponseModel>
            {
                new UpdatedNpwdPrnsResponseModel
                {
                    EvidenceNo = "12345",
                    EvidenceStatusCode = "EV-ACANCEL",
                },
                new UpdatedNpwdPrnsResponseModel
                {
                    EvidenceNo = "67890",
                    EvidenceStatusCode = "EV-ACCEP",
                },
            };

            // Act
            var result = PrnMapper.Map(updatedPrns, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://fat.npwd.org.uk/odata/PRNs/$delta", result.Context);
            Assert.Equal(2, result.Value.Count);

            Assert.Equal("12345", result.Value[0].EvidenceNo);
            Assert.Equal("EV-ACANCEL", result.Value[0].EvidenceStatusCode);
            Assert.Equal("2025", result.Value[0].ObligationYear);

            Assert.Equal("67890", result.Value[1].EvidenceNo);
            Assert.Equal("EV-ACCEP", result.Value[1].EvidenceStatusCode);
            Assert.Equal("2025", result.Value[1].ObligationYear);
        }

        [Fact]
        public void Map_ValidInput_EmptyEvidenceStatusCode_SuccessfulMapping()
        {
            // Arrange
            var updatedPrns = new List<UpdatedNpwdPrnsResponseModel>
            {
                new UpdatedNpwdPrnsResponseModel { EvidenceNo = "12345", EvidenceStatusCode = "" },
            };

            // Act
            var result = PrnMapper.Map(updatedPrns, _configurationMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://fat.npwd.org.uk/odata/PRNs/$delta", result.Context);
            Assert.Single(result.Value);

            // Check first entry
            Assert.Equal("12345", result.Value[0].EvidenceNo);
            Assert.Equal("", result.Value[0].EvidenceStatusCode);
        }

        [Theory]
        [InlineData(null, "2025")]
        [InlineData("2026", "2026")]
        public void Map_ValidInput_ObligationYear_FromInputOrConfigFallback(
            string? obligationYear,
            string expectedObligationYear
        )
        {
            var updatedPrns = new List<UpdatedNpwdPrnsResponseModel>
            {
                new()
                {
                    EvidenceNo = "12345",
                    EvidenceStatusCode = "",
                    ObligationYear = obligationYear,
                },
            };

            var result = PrnMapper.Map(updatedPrns, _configurationMock.Object);

            Assert.Equal(expectedObligationYear, result.Value[0].ObligationYear);
        }
    }
}
