using EprPrnIntegration.Common.Models;
using FluentValidation;

namespace EprPrnIntegration.Common.Validators
{
    public class NpwdPrnValidator : AbstractValidator<NpwdPrn>
    {
        public NpwdPrnValidator()
        {
            // 1.Accreditation Number not blank
            RuleFor(prn => prn.AccreditationNo).NotNull().NotEmpty();

            // 2.PRN Number not blank
            RuleFor(prn => prn.EvidenceNo).NotNull().NotEmpty();

            // 3.EprID not blank
            // 4.EprID - need to verify from pEPR whether given Id exists in pEPR whether its an organisation or Compliance
            RuleFor(prn => prn.IssuedToEPRId).Must(BeValidIssuedToEPRId).WithMessage("IssuedToEPRId must match a PEPR organisatoin Id");

            // 5.Tonnage - is integer greater than zero
            RuleFor(prn => prn.EvidenceTonnes).GreaterThan(0);

            // 6a.Material Name: Should not be blank
            RuleFor(prn => prn.EvidenceMaterial).NotNull().NotEmpty();

            // 6b.and should be a text value and should match Material Names List
            var validMaterials = new List<string>() { "Aluminium", "Glass", "Paper", "Plastic", "Steel", "Wood" };
            RuleFor(prn => prn.EvidenceMaterial)
                .Must(material => validMaterials.Contains(material ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Material must be one of: " + String.Join(", ", validMaterials));

            // 7.AccreditationYear: Should should be valid 4 digit integer(Year)
            int minYear = 2025;
            int maxYear = DateTime.UtcNow.Year + 1;
            RuleFor(prn => prn.AccreditationYear).InclusiveBetween(minYear, maxYear);

            // 8.If the status is cancelled then there should be a valid date in field Cancelled Date.
            RuleFor(prn => prn.CancelledDate).NotNull()
                .When(prn => prn?.EvidenceStatusCode?.ToUpper() == "EV-CANCEL")
                .WithMessage("Cancellation date must not be null when PRN has status of EV-CANCEL");

            RuleFor(prn => prn.CancelledDate).Null()
                .When(prn => prn?.EvidenceStatusCode?.ToUpper() != "EV-CANCEL")
                .WithMessage("Cancellation date must be null when PRN is not cancelled");

            // 9.IssueDate not blank
            RuleFor(prn => prn.IssueDate).NotNull();

        }

        private bool BeValidIssuedToEPRId(NpwdPrn prn, string? eprId)
        {

            if (string.IsNullOrWhiteSpace(eprId))
            {
                return false;
            }

            if (prn.ValidOrganisationIds == null)
            {
                return false;
            }

            if (!Guid.TryParse(eprId, out Guid orgId))
            {
                return false;
            }

            return prn.ValidOrganisationIds.Contains(orgId);

        }
    }
}
