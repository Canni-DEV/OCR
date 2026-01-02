namespace Ocr.Api.Options;

public class AzureOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public int RetryCount { get; set; } = 3;

    public int ResetDay { get; set; } = 1;

    public int HardLimit { get; set; } = 50000;
}
