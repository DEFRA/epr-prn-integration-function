using EprPrnIntegration.Common.Constants;
using EprPrnIntegration.Common.Helpers;
using EprPrnIntegration.Common.Mappers;
using EprPrnIntegration.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EprPrnIntegration.Tests.Mappers
{
    public class NpwdPrnToSavePrnDetailsRequestMapperTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger> _loggerMock;

        public NpwdPrnToSavePrnDetailsRequestMapperTests()
        {
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger>();

            // Default: no explicit DefaultObligationYear configured
            _configMock.Setup(c => c["DefaultObligationYear"]).Returns((string?)null);
        }

        [Fact]
        public void Map_ValidNpwdPrn_ReturnsMappedSavePrnDetailsRequest()
        {
            // Arrange
            _configMock.Setup(c => c["DefaultObligationYear"]).Returns("2025"); // not used, but valid

            var npwdPrn = new NpwdPrn
            {
                AccreditationNo = "12345",
                AccreditationYear = 2024,
                CancelledDate = DateTimeHelper.NewUtcDateTime(2024, 12, 11),
                DecemberWaste = true,
                EvidenceMaterial = "Plastic",
                EvidenceNo = "EA123456",
                EvidenceStatusCode = "Active",
                EvidenceTonnes = 200,
                IssueDate = DateTimeHelper.NewUtcDateTime(2024, 01, 01),
                IssuedByNPWDCode = "6B29FC40-CA47-1067-B31D-00DD010662DA",
                IssuedByOrgName = "Exporter Ltd.",
                IssuedToNPWDCode = "6B29FC40-CA47-1067-B31D-00DD010662DA",
                IssuedToOrgName = "Reprocessor Ltd.",
                IssuedToEPRId = "6b29fc40-ca47-1067-b31d-00dd010662da",
                IssuerNotes = "No Notes",
                IssuerRef = "IssuerRef123",
                MaterialOperationCode = "6b29fc40-ca47-1067-b31d-00dd010662da",
                ModifiedOn = DateTimeHelper.NewUtcDateTime(2024, 11, 01),
                ObligationYear = 2024,
                PRNSignatory = "John Doe",
                PRNSignatoryPosition = "Manager",
                ProducerAgency = "AgencyA",
                RecoveryProcessCode = "R123",
                ReprocessorAgency = "AgencyB",
                StatusDate = DateTimeHelper.NewUtcDateTime(2024, 11, 10),
            };

            // Act
            var result = NpwdPrnToSavePrnDetailsRequestMapper.Map(
                npwdPrn,
                _configMock.Object,
                _loggerMock.Object
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(npwdPrn.AccreditationNo, result.AccreditationNo);
            Assert.Equal(npwdPrn.AccreditationYear.ToString(), result.AccreditationYear);
            Assert.Equal(npwdPrn.CancelledDate, result.CancelledDate);
            Assert.Equal(npwdPrn.DecemberWaste, result.DecemberWaste);
            Assert.Equal(npwdPrn.EvidenceMaterial, result.EvidenceMaterial);
            Assert.Equal(npwdPrn.EvidenceNo, result.EvidenceNo);
            Assert.Equal(
                NpwdStatusToPrnStatusMapper.Map(npwdPrn.EvidenceStatusCode!),
                result.EvidenceStatusCode
            );
            Assert.Equal(npwdPrn.EvidenceTonnes, result.EvidenceTonnes);
            Assert.Equal(npwdPrn.IssueDate, result.IssueDate);
            Assert.Equal(Guid.Parse(npwdPrn.IssuedByNPWDCode), result.IssuedByNPWDCode);
            Assert.Equal(npwdPrn.IssuedByOrgName, result.IssuedByOrgName);
            Assert.Equal(Guid.Parse(npwdPrn.IssuedToNPWDCode), result.IssuedToNPWDCode);
            Assert.Equal(npwdPrn.IssuedToOrgName, result.IssuedToOrgName);
            Assert.Equal(Guid.Parse(npwdPrn.IssuedToEPRId), result.IssuedToEPRId);

            // NOTE: mapper now maps IssuerNotes and IssuerRef separately
            Assert.Equal(npwdPrn.IssuerNotes, result.IssuerNotes);
            Assert.Equal(npwdPrn.IssuerRef, result.IssuerRef);

            Assert.Equal(Guid.Parse(npwdPrn.MaterialOperationCode), result.MaterialOperationCode);
            Assert.Equal(npwdPrn.ModifiedOn, result.ModifiedOn);
            Assert.Equal(npwdPrn.ObligationYear.ToString(), result.ObligationYear);
            Assert.Equal(npwdPrn.PRNSignatory, result.PrnSignatory);
            Assert.Equal(npwdPrn.PRNSignatoryPosition, result.PrnSignatoryPosition);
            Assert.Equal(npwdPrn.ProducerAgency, result.ProducerAgency);
            Assert.Equal(npwdPrn.RecoveryProcessCode, result.RecoveryProcessCode);
            Assert.Equal(npwdPrn.ReprocessorAgency, result.ReprocessorAgency);
            Assert.Equal(npwdPrn.StatusDate, result.StatusDate);
            Assert.Equal("IntegrationFA", result.CreatedByUser);
        }

        [Fact]
        public void Map_WithNullObligationYear_UsesConfiguredDefault_WhenValid()
        {
            // Arrange
            _configMock.Setup(c => c["DefaultObligationYear"]).Returns("2026");

            var npwdPrn = new NpwdPrn { EvidenceNo = "EA123456", ObligationYear = null };

            // Act
            var result = NpwdPrnToSavePrnDetailsRequestMapper.Map(
                npwdPrn,
                _configMock.Object,
                _loggerMock.Object
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2026", result.ObligationYear);
        }

        [Fact]
        public void Map_WithNullObligationYear_AndMissingConfig_UsesDefaultConstant()
        {
            // Arrange
            _configMock.Setup(c => c["DefaultObligationYear"]).Returns((string?)null);

            var npwdPrn = new NpwdPrn { EvidenceNo = "EA123456", ObligationYear = null };

            // Act
            var result = NpwdPrnToSavePrnDetailsRequestMapper.Map(
                npwdPrn,
                _configMock.Object,
                _loggerMock.Object
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ObligationYearDefaults.ObligationYear2025, result.ObligationYear);
        }

        [Fact]
        public void Map_WithNullObligationYear_AndInvalidConfig_UsesDefaultConstant_AndLogsWarning()
        {
            // Arrange
            _configMock.Setup(c => c["DefaultObligationYear"]).Returns("not-a-year");

            var npwdPrn = new NpwdPrn { EvidenceNo = "EA123456", ObligationYear = null };

            // Act
            var result = NpwdPrnToSavePrnDetailsRequestMapper.Map(
                npwdPrn,
                _configMock.Object,
                _loggerMock.Object
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ObligationYearDefaults.ObligationYear2025, result.ObligationYear);

            // Optional: verify a warning was logged at least once
            _loggerMock.Verify(
                x =>
                    x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception?>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                    ),
                Times.AtLeastOnce
            );
        }

        [Fact]
        public void Map_WithNullIssuerRef_MapsIssuerRefToEmptyString()
        {
            // Arrange
            var npwdPrn = new NpwdPrn
            {
                EvidenceNo = "EA123456",
                IssuerNotes = "Some notes",
                IssuerRef = null,
                ObligationYear = 2024,
            };

            // Act
            var result = NpwdPrnToSavePrnDetailsRequestMapper.Map(
                npwdPrn,
                _configMock.Object,
                _loggerMock.Object
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Some notes", result.IssuerNotes);
            Assert.Equal(string.Empty, result.IssuerRef);
        }

        [Theory]
        [InlineData("EX123456", true)] // EA export prefix
        [InlineData("SXPA123456", true)] // SEPA export prefix
        [InlineData("XYZ123456", false)] // Not an export prefix
        [InlineData("", false)] // Empty string
        [InlineData(null, false)] // Null string
        public void IsExport_ReturnsExpectedResult(string evidenceNo, bool expectedResult)
        {
            // Act
            var result = NpwdPrnToSavePrnDetailsRequestMapper.IsExport(evidenceNo);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
