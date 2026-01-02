namespace Ocr.Api.Options;

/// <summary>
/// Configuration for simple in-memory rate limiting.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of requests allowed in a window.
    /// </summary>
    public int PermitLimit { get; set; } = 60;

    /// <summary>
    /// Gets or sets the size of the rate limit window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
