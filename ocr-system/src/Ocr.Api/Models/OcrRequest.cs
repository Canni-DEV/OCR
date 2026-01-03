using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Ocr.Api.Models;

/// <summary>
/// Represents the multipart/form-data payload for an OCR request.
/// </summary>
public class OcrRequest
{
    /// <summary>
    /// File to process.
    /// </summary>
    [Required]
    public IFormFile? File { get; set; }
}
