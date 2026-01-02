namespace Ocr.Api.Models;

/// <summary>
/// Represents normalized and enriched OCR data.
/// </summary>
public class ProcessedTextResult
{
    /// <summary>
    /// Gets or sets the normalized text after post-processing.
    /// </summary>
    public string NormalizedText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional CUIT detected in the text.
    /// </summary>
    public string? DetectedCuit { get; set; }

    /// <summary>
    /// Gets or sets an optional remittance number detected in the text.
    /// </summary>
    public string? NumeroRemito { get; set; }

    /// <summary>
    /// Gets or sets the detected customer or vendor name if available.
    /// </summary>
    public string? CustomerOrVendor { get; set; }
}
