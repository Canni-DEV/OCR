namespace Ocr.Api.Options;

public class WorkerOptions
{
    public int AcquireTimeoutSeconds { get; set; } = 120;

    public List<string> Endpoints { get; set; } = new();

    public int MaxAttempts { get; set; } = 3;
}
