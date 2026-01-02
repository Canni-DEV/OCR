namespace Ocr.Api.Models;

public class ProcessedTextResult
{
    public string NormalizedText { get; set; } = string.Empty;

    public string? DetectedCuit { get; set; }

    public string? NumeroRemito { get; set; }

    public string? CustomerOrVendor { get; set; }
}
