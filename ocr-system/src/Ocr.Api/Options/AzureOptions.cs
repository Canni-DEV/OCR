namespace Ocr.Api.Options;

/// <summary>
/// Configuration settings for Azure Cognitive Services.
/// </summary>
public class AzureOptions
{
    /// <summary>
    /// Gets or sets the Azure Read endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key used to authenticate requests.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout, in seconds, for Azure calls.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of retries for Azure requests.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the day of the month when usage counters reset.
    /// </summary>
    public int ResetDay { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum allowed calls within a billing period.
    /// </summary>
    public int HardLimit { get; set; } = 50000;
}
