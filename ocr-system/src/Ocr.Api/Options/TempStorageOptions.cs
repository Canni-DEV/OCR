namespace Ocr.Api.Options;

/// <summary>
/// Settings for temporary storage used during OCR processing.
/// </summary>
public class TempStorageOptions
{
    /// <summary>
    /// Gets or sets the root directory for temporary files.
    /// </summary>
    public string Root { get; set; } = "./temp";
}
