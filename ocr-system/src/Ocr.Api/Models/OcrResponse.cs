namespace Ocr.Api.Models;

/// <summary>
/// API response containing OCR output and metadata.
/// </summary>
public class OcrResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw extracted text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OCR engine that produced the text.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the post-processed OCR result.
    /// </summary>
    public ProcessedTextResult Processed { get; set; } = new();

    /// <summary>
    /// Gets or sets the total elapsed time in milliseconds.
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Gets or sets an optional correlation identifier for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}
