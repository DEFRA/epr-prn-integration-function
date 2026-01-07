namespace EprPrnIntegration.Common.Constants
{
    public static class ExceptionMessages
    {
        // HttpGovPayService exceptions
        public const string OrganisationServiceBaseUrlMissing =
            "OrganisationService BaseUrl configuration is missing";
        public const string OrganisationServiceEndPointNameMissing =
            "OrganisationService EndPointName configuration is missing";
        public const string PrnServiceBaseUrlMissing =
            "PrnService BaseUrl configuration is missing";
        public const string PrnServiceEndPointNameMissing =
            "PrnService EndPointName configuration is missing";
        public const string PrnServiceEndPointNameV2Missing =
            "PrnService EndPointNameV2 configuration is missing";
        public const string CommonDataServiceBaseUrlMissing =
            "CommonDataService BaseUrl configuration is missing";
        public const string CommonDataServiceEndPointNameMissing =
            "CommonDataService EndPointName configuration is missing";
        public const string WasteOrganisationsApiBaseUrlMissing =
            "WasteOrganisationsApi BaseUrl configuration is missing";
        public const string RrepwApiBaseUrlMissing = "RrepwApi BaseUrl configuration is missing";

        // HttpGovPayService exceptions
        public const string BearerTokenNull = "Bearer token is null. Unable to initiate payment.";
        public const string GovPayResponseInvalid =
            "GovPay response does not contain a valid PaymentId.";
        public const string PaymentStatusNotFound =
            "Payment status not found or status is not available.";
        public const string ErrorInitiatingPayment = "Error occurred while initiating payment.";
        public const string ErrorRetrievingPaymentStatus =
            "Error occurred while retrieving payment status.";

        // HttpPaymentsService exceptions
        public const string PaymentServiceBaseUrlMissing =
            "PaymentService BaseUrl configuration is missing";
        public const string PaymentServiceEndPointNameMissing =
            "PaymentService EndPointName configuration is missing";
        public const string PaymentServiceHttpClientNameMissing =
            "PaymentService HttpClientName configuration is missing";
        public const string ErrorInsertingPayment =
            "Error occurred while inserting payment status.";
        public const string ErrorUpdatingPayment = "Error occurred while updating payment status.";
        public const string UnexpectedErrorInsertingPayment =
            "An unexpected error occurred while inserting the payment.";
        public const string UnexpectedErrorUpdatingPayment =
            "An unexpected error occurred while updating the payment status.";
        public const string ErrorRetrievingPaymentDetails =
            "Error occurred while retrieving payment details.";
        public const string ErrorGettingPaymentDetails =
            "Error occurred while getting payment details.";

        // PaymentsService exceptions
        public const string ReturnUrlNotConfigured = "ReturnUrl is not configured.";
        public const string DescriptionNotConfigured = "Description is not configured.";
        public const string GovPayPaymentIdNull = "GovPayPaymentId cannot be null or empty";
        public const string SuccessStatusWithErrorCode =
            "Error code should be null or empty for a success status.";
        public const string FailedStatusWithoutErrorCode =
            "Error code cannot be null or empty for a failed status.";
        public const string ErrorStatusWithoutErrorCode =
            "Error code cannot be null or empty for an error status.";

        // BaseHttpService exceptions
        public const string ApiResponseError =
            "Error occurred calling API with error code: {0}. Message: {1}";

        // PaymentsController validation messages
        public const string AmountMustBeGreaterThanZero = "Amount must be greater than 0";

        // HttpRegistrationFeesService exceptions
        public const string RegistrationFeesServiceBaseUrlMissing =
            "RegistrationFeesService BaseUrl configuration is missing";
        public const string RegistrationFeesServiceEndPointNameMissing =
            "RegistrationFeesService EndPointName configuration is missing";
        public const string RegistrationFeesServiceHttpClientNameMissing =
            "RegistrationFeesService HttpClientName configuration is missing";
        public const string ErrorCalculatingProducerFees =
            "Error occurred while calculating producer registration fees.";
        public const string UnexpectedErrorCalculatingProducerFees =
            "An unexpected error occurred while calculating producer registration fees.";
        public const string ErrorResubmissionFees =
            "Error occurred while getting resubmission fee.";

        // ProducerFeesService exceptions
        public const string RegulatorCanNotBeNullOrEmpty = "regulator cannot be null or empty";

        // ProducersFeesController specific exceptions
        public const string UnexpectedErrorCalculatingFees =
            "An unexpected error occurred while calculating the fees.";

        // ComplianceSchemeFeesService exceptions
        public const string ComplianceSchemeServiceUrlMissing =
            "ComplianceSchemeService url configuration is missing.";
        public const string ComplianceSchemeServiceEndPointNameMissing =
            "ComplianceSchemeService EndPointName configuration is missing.";
        public const string ComplianceSchemeServiceHttpClientNameMissing =
            "ComplianceSchemeService HttpClientName configuration is missing.";
        public const string ErrorCalculatingComplianceSchemeFees =
            "Error occurred while calculating Compliance fees.";
        public const string UnexpectedErrorCalculatingComplianceSchemeFees =
            "An unexpected error occurred while calculating Compliance Scheme fees.";
    }
}
