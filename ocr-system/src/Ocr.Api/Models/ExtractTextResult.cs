namespace Ocr.Api.Models;

/// <summary>
/// Result returned by an OCR engine.
/// </summary>
public class ExtractTextResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the OCR call succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the extracted text content, if available.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets an error message when extraction fails.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time in milliseconds for the operation.
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// Gets or sets the endpoint that processed the request.
    /// </summary>
    public string SourceEndpoint { get; set; } = string.Empty;
}
