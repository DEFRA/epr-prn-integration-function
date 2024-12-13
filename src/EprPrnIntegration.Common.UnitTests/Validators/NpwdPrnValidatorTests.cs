using AutoFixture;
using EprPrnIntegration.Common.Models;
using EprPrnIntegration.Common.Validators;
using FluentValidation.TestHelper;

namespace EprPrnIntegration.Common.UnitTests.Validators
{
    public class NpwdPrnValidatorTests
    {
        private NpwdPrnValidator _sut = null!;
        private Fixture _fixture = new Fixture();
        public NpwdPrnValidatorTests()
        {
            _sut = new NpwdPrnValidator();
        }

        // Accreditation No
        [Fact]
        public void Should_Not_Have_Error_When_AccreditationNo_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.AccreditationNo);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_Have_Error_When_AccreditationNo_Is_NulllOrEmpty(string? npwdAccreditationNo)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.AccreditationNo = npwdAccreditationNo;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.AccreditationNo);
        }

        // EvidenceNo, equates to PRN Number
        [Fact]
        public void Should_Not_Have_Error_When_EvidenceNo_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceNo);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_Have_Error_When_EvidenceNo_Is_NulllOrEmpty(string? npwdEvidenceNo)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceNo = npwdEvidenceNo;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceNo);
        }

        // IssuedToEPRId, equates to organisation id
        [Fact]
        public void Should_Not_Have_Error_When_IssuedEPRId_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.IssuedToEPRId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Should_Have_Error_When_IssuedEPRId_Is_NulllOrEmpty(string? npwdEprId)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssuedToEPRId = npwdEprId;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssuedToEPRId);
        }

        // Tonnage
        [Fact]
        public void Should_Not_Have_Error_When_Tonnage_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceTonnes);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Should_Have_Error_When_Tonnage_Is_Not_Valid(int npwdTonnage)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceTonnes = npwdTonnage;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceTonnes);
        }

        // Material e.g. paper
        [Theory]
        [InlineData("Aluminium")]
        [InlineData("Glass")]
        [InlineData("Paper")]
        [InlineData("Plastic")]
        [InlineData("Steel")]
        [InlineData("Wood")]
        [InlineData("aluminium")]
        public void Should_Not_Have_Error_When_EvidenceMaterial_Is_Valid(string? npwdMaterial)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceMaterial = npwdMaterial;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.EvidenceMaterial);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Zinc")]
        public void Should_Have_Error_When_EvidenceMaterial_Is_NulllOrEmpty(string? npwdMaterial)
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceMaterial = npwdMaterial;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceMaterial);
        }

        // Accreditation Year
        private const int MinAccreditationYear = 2025;
        [Fact]
        public void Should_Not_Have_Error_When_AccrediationYear_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            int maxYear =  DateTime.UtcNow.Year + 1;
            for (int year = MinAccreditationYear; year < maxYear; year++)
            {
                npwdPrn.AccreditationYear = year;
                var result = _sut.TestValidate(npwdPrn);
                result.ShouldNotHaveValidationErrorFor(x => x.AccreditationYear);
            }
        }

        [Fact]
        public void Should_Have_Error_When_AccreditationYear_Is_Out_Of_Bounds()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.AccreditationYear = MinAccreditationYear - 1;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.AccreditationYear);

            npwdPrn.AccreditationYear = MinAccreditationYear + 2;
            result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.AccreditationYear);
        }

        // Cancelled Date
        [Fact]
        public void Should_Not_Have_Error_When_CancelledDate_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.CancelledDate = null;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.CancelledDate);

            npwdPrn.EvidenceStatusCode = "EV-CANCEL";
            npwdPrn.CancelledDate = DateTime.UtcNow;
            result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.CancelledDate);
        }

        [Fact]
        public void Should_Have_Error_When_CancelledDate_Is_Null_And_Status_Is_Not_Cancelled()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.EvidenceStatusCode = "EV-AWACCEP";
            npwdPrn.CancelledDate = DateTime.UtcNow;

            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.CancelledDate);
        }

        // Issue Date
        [Fact]
        public void Should_Not_Have_Error_When_IssueDate_Is_Valid()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssueDate = DateTime.UtcNow;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldNotHaveValidationErrorFor(x => x.IssueDate);
        }

        [Fact]
        public void Should_Have_Error_When_IssueDate_Is_Nulll()
        {
            var npwdPrn = _fixture.Create<NpwdPrn>();
            npwdPrn.IssueDate = null;
            var result = _sut.TestValidate(npwdPrn);
            result.ShouldHaveValidationErrorFor(x => x.IssueDate);
        }

    }
}
