namespace Ocr.Api.Models;

public class AuditRecord
{
    public string RequestId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long Length { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public IEnumerable<string> EndpointsTried { get; set; } = Enumerable.Empty<string>();
}
