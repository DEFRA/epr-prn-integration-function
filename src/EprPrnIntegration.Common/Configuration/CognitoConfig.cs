namespace EprPrnIntegration.Common.Configuration;

public class CognitoConfig : ICognitoConfiguration
{
    public string AccessTokenUrl { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}
