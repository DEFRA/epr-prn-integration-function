namespace EprPrnIntegration.Common.Constants
{
    public static class Values
    {
        public const string ExceptionLogMessage = "GOV UK NOTIFY ERROR. Method: SendEmail, Organisation ID: {OrganisationId}, Template: {TemplateId}";
    }

    public static class Constants
    {
        public const string ApplicationName = "PRN Integration Function Application";
    }

    public static class HttpClientNames
    {
        public const string Npwd = "NpwdClient";
        public const string Prn = "PrnClient";
        public const string Organisation = "OrganisationClient";
        public const string Account = "AccountClient";
        public const string CommonData = "CommonDataClient";
    }

    public static class HttpHeaderNames
    {
        public const string Accept = "Accept";
        public const string Bearer = "Bearer";
    }

    public static class ConfigSettingKeys
    {
        public const string KeyVaultUrl = "AzureKeyVaultUrl";

        public static class NpwdOAuth
        {
            public const string ClientId = "NPWDIntegrationClientID";
            public const string ClientSecret = "NPWDIntegrationClientSecret";
            public const string Authority = "NPWDAuthority";
            public const string Scope = "NPWDScope";
            public const string AccessTokenUrl = "NPWDAccessTokenURL";
            public const string TokenName = "NPWDTokenName";
            public const string ApiBaseUrl = "NpwdApiBaseUrl";
        }
    }

    public static class NpwdApiPath
    {
        public const string UpdateProducers = "odata/producers";
        public const string UpdatePrns = "odata/PRNs";
    }

    public static class ExporterCodePrefixes
    {
        public const string EaExport = "EX";
        public const string SepaExport = "SX";
    }

    public static class CustomEvents
    {
        public const string IssuedPrn = "IssuedPrn";
        public const string UpdateProducer = "UpdateProducer";
        public const string NpwdPrnValidationError = "NpwdPrnValidationError";
    }

    public static class CustomEventFields
    {
        public const string PrnNumber = "PRN Number";
        public const string IncomingStatus = "Incoming Status";
        public const string Date = "Date";
        public const string OrganisationName = "Organisation Name";
        public const string ErrorComments = "Error Comments";
    }
}
