namespace Ocr.Api.Models;

/// <summary>
/// Represents a persisted audit entry for an OCR request.
/// </summary>
public class AuditRecord
{
    /// <summary>
    /// Gets or sets the unique identifier associated with the OCR request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original uploaded file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the uploaded file size in bytes.
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// Gets or sets the OCR engine that produced the result.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the request was processed.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the list of Paddle worker endpoints attempted.
    /// </summary>
    public IEnumerable<string> EndpointsTried { get; set; } = Enumerable.Empty<string>();
}
