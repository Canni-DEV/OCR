namespace Ocr.Api.Options;

/// <summary>
/// General OCR processing defaults.
/// </summary>
public class OcrOptions
{
    /// <summary>
    /// Gets or sets the default language code for OCR requests.
    /// </summary>
    public string DefaultLanguage { get; set; } = "es";
}
