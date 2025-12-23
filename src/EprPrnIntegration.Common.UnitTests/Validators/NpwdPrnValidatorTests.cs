using AutoFixture;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.RESTServices.BackendAccountService.Interfaces;
using EprPrnIntegration.Common.Validators;
using FluentValidation.TestHelper;
using Moq;

namespace EprPrnIntegration.Common.UnitTests.Validators
{
    public class NpwdPrnValidatorTests
    {
        private NpwdPrnValidator _sut = null!;
        private Fixture _fixture = new Fixture();
        private readonly Mock<IOrganisationService> _mockOrganisationService;

        public NpwdPrnValidatorTests()
        {
            _mockOrganisationService = new Mock<IOrganisationService>();

            _sut = new NpwdPrnValidator(_mockOrganisationService.Object);
        }

        // Accreditation No
        [Fact]
        public async Task AccreditationNo_Should_Not_Have_Error_When_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.AccreditationNo);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task AccreditationNo_Should_Have_Error_When_Is_NulllOrEmpty(
            string? npwdAccreditationNo
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.AccreditationNo = npwdAccreditationNo;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.AccreditationNo);
        }

        // EvidenceNo, equates to PRN Number
        [Fact]
        public async Task EvidenceNo_Should_Not_Have_Error_When_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceNo);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EvidenceNo_Should_Have_Error_When_Is_NulllOrEmpty(string? npwdEvidenceNo)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceNo = npwdEvidenceNo;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceNo);
        }

        // IssuedToEPRId, equates to organisation id
        [Fact]
        public async Task IssuedEPRId_Should_Not_Have_Error_When_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var orgId = Guid.NewGuid();
            npwdPrn.IssuedToEPRId = orgId.ToString();

            _mockOrganisationService
                .Setup(service =>
                    service.DoesProducerOrComplianceSchemeExistAsync(
                        npwdPrn.IssuedToEPRId,
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(true);

            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.IssuedToEPRId);
            _mockOrganisationService.Verify(
                provider =>
                    provider.DoesProducerOrComplianceSchemeExistAsync(
                        npwdPrn.IssuedToEPRId,
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task IssuedEPRId_Should_Have_Error_When_Is_NulllOrEmpty(string? npwdEprId)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssuedToEPRId = npwdEprId;

            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssuedToEPRId);
        }

        [Theory]
        [InlineData("OrganisationId")]
        [InlineData("05be0802-19ac-4c80-b99d-6452577bf93d")]
        public async Task IssuedEPRId_Should_Have_Error_When_Not_Guid_Or_Invalid_Guid(
            string? npwdEprId
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssuedToEPRId = npwdEprId;

            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssuedToEPRId);
        }

        [Fact]
        public async Task IssuedEPRId_Should_Have_Error_When_OrganisationServiceThrowExceptionGuid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var orgId = Guid.NewGuid();
            npwdPrn.IssuedToEPRId = orgId.ToString();

            _mockOrganisationService
                .Setup(service =>
                    service.DoesProducerOrComplianceSchemeExistAsync(
                        npwdPrn.IssuedToEPRId,
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new Exception());

            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssuedToEPRId);
        }

        // Tonnage
        [Fact]
        public async Task Tonnage_Should_Not_Have_Error_When_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceTonnes);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task Tonnage_Should_Have_Error_When_Is_Not_Valid(int npwdTonnage)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceTonnes = npwdTonnage;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceTonnes);
        }

        // Material e.g. wood
        [Theory]
        [InlineData("Aluminium")]
        [InlineData("Glass Other")]
        [InlineData("Glass Re-melt")]
        [InlineData("Paper/board")]
        [InlineData("Plastic")]
        [InlineData("Steel")]
        [InlineData("Wood")]
        [InlineData("aluminium")]
        public async Task EvidenceMaterial_Should_Not_Have_Error_When_Is_Valid(string? npwdMaterial)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceMaterial = npwdMaterial;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceMaterial);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Zinc")]
        public async Task EvidenceMaterial_Should_Have_Error_When_Is_NulllOrEmpty_Or_InvalidMaterial(
            string? npwdMaterial
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceMaterial = npwdMaterial;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceMaterial);
        }

        // Accreditation Year
        private const int MinAccreditationYear = 2024;

        [Fact]
        public async Task AccrediationYear_Should_Not_Have_Error_When_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            int maxYear = DateTime.UtcNow.Year + 1;
            for (int year = MinAccreditationYear; year < maxYear; year++)
            {
                npwdPrn.AccreditationYear = year;
                var result = await _sut.TestValidateAsync(npwdPrn);
                result.ShouldNotHaveValidationErrorFor(x => x.AccreditationYear);
            }
        }

        [Fact]
        public async Task AccreditationYear_Should_Have_Error_When_Is_Out_Of_Bounds()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.AccreditationYear = MinAccreditationYear - 1;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.AccreditationYear);

            npwdPrn.AccreditationYear = DateTime.UtcNow.Year + 2;
            result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.AccreditationYear);
        }

        // Cancelled Date
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("EV-AWACCEP")]
        public async Task CancelledDate_Should_Not_Have_Error_When_Is_Null_And_Status_Is_Not_Cancelled(
            string statusCode
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceStatusCode = statusCode;
            npwdPrn.CancelledDate = null;

            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.CancelledDate);
        }

        [Theory]
        [InlineData("EV-CANCEL")]
        [InlineData("ev-cancel")]
        public async Task CancelledDate_Should_Not_Have_Error_When_Is_Not_Null_And_Status_Is_Cancelled(
            string statusCode
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceStatusCode = statusCode;
            npwdPrn.CancelledDate = DateTime.UtcNow;

            var result = await _sut.TestValidateAsync(npwdPrn);
            result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.CancelledDate);
        }

        [Theory]
        [InlineData("EV-CANCEL")]
        [InlineData("ev-cancel")]
        public async Task CancelledDate_Should_Have_Error_When_Is_Null_And_Status_Is_Cancelled(
            string statusCode
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceStatusCode = statusCode;
            npwdPrn.CancelledDate = null;

            var result = await _sut.TestValidateAsync(npwdPrn);
            result
                .ShouldHaveValidationErrorFor(x => x.CancelledDate)
                .WithErrorMessage(
                    "Cancellation date must not be null when PRN has status of EV-CANCEL"
                );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("EV-AWACCEP")]
        public async Task CancelledDate_Should_Have_Error_When_Is_Not_Null_And_Status_Is_Not_Cancelled(
            string statusCode
        )
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceStatusCode = statusCode;
            npwdPrn.CancelledDate = DateTime.UtcNow;

            var result = await _sut.TestValidateAsync(npwdPrn);
            result
                .ShouldHaveValidationErrorFor(x => x.CancelledDate)
                .WithErrorMessage("Cancellation date must be null when PRN is not cancelled");
        }

        // Issue Date
        [Fact]
        public async Task IssueDate_Should_Not_Have_Error_When_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssueDate = DateTime.UtcNow;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.IssueDate);
        }

        [Fact]
        public async Task IssueDate_Should_Have_Error_When_Is_Nulll()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssueDate = null;
            var result = await _sut.TestValidateAsync(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssueDate);
        }
    }
}
