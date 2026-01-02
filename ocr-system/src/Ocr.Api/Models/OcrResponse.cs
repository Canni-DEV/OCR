namespace Ocr.Api.Models;

public class OcrResponse
{
    public string RequestId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public ProcessedTextResult Processed { get; set; } = new();

    public long ElapsedMs { get; set; }

    public string? CorrelationId { get; set; }
}
