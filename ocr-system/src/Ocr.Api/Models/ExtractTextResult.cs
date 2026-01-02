namespace Ocr.Api.Models;

public class ExtractTextResult
{
    public bool Success { get; set; }

    public string? Text { get; set; }

    public string? Error { get; set; }

    public long ElapsedMs { get; set; }

    public string SourceEndpoint { get; set; } = string.Empty;
}
