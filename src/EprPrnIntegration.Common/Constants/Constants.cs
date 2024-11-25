namespace EprPrnIntegration.Common.Constants
{
    public static class HttpClientNames
    {
        public const string NpwdGet = "NpwdClientGet";
        public const string NpwdPush = "NpwdClientPush";
    }

    public static class HttpHeaderNames
    {
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
    }
}
