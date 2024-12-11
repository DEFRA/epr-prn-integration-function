using EprPrnIntegration.Common.Enums;
using EprPrnIntegration.Common.Mappers;
using Xunit;

namespace EprPrnIntegration.Tests.Mappers
{
    public class NpwdStatusToPrnStatusMapperTests
    {
        [Fact]
        public void Map_EVACCEP_ReturnsAccepted()
        {
            // Arrange
            var npwdStatus = "EV-ACCEP";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(EprnStatus.ACCEPTED, result);
        }

        [Fact]
        public void Map_EVACANCEL_ReturnsRejected()
        {
            // Arrange
            var npwdStatus = "EV-ACANCEL";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(EprnStatus.REJECTED, result);
        }

        [Fact]
        public void Map_EVCANCEL_ReturnsCancelled()
        {
            // Arrange
            var npwdStatus = "EV-CANCEL";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(EprnStatus.CANCELLED, result);
        }

        [Fact]
        public void Map_EVAWACCEP_ReturnsAwaitingAcceptance()
        {
            // Arrange
            var npwdStatus = "EV-AWACCEP";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(EprnStatus.AWAITINGACCEPTANCE, result);
        }

        [Fact]
        public void Map_EVAWACCEP_EPR_ReturnsAwaitingAcceptance()
        {
            // Arrange
            var npwdStatus = "EV-AWACCEP-EPR";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(EprnStatus.AWAITINGACCEPTANCE, result);
        }

        [Fact]
        public void Map_InvalidStatus_ReturnsNull()
        {
            // Arrange
            var npwdStatus = "EV-INVALID";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_NullStatus_ReturnsNull()
        {
            // Arrange
            string npwdStatus = null;

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_EmptyStatus_ReturnsNull()
        {
            // Arrange
            var npwdStatus = "";

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_MixedCaseStatus_ReturnsCorrectEnum()
        {
            // Arrange
            var npwdStatus = "ev-accep"; // Testing lower case

            // Act
            var result = NpwdStatusToPrnStatusMapper.Map(npwdStatus);

            // Assert
            Assert.Equal(EprnStatus.ACCEPTED, result);
        }
    }
}
