namespace Ocr.Api.Options;

public class RateLimitOptions
{
    public bool Enabled { get; set; }

    public int PermitLimit { get; set; } = 60;

    public int WindowSeconds { get; set; } = 60;
}
