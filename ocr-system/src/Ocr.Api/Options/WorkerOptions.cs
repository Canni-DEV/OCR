namespace Ocr.Api.Options;

/// <summary>
/// Configuration for managing the pool of PaddleOCR workers.
/// </summary>
public class WorkerOptions
{
    /// <summary>
    /// Gets or sets the maximum time, in seconds, to wait for a worker lease.
    /// </summary>
    public int AcquireTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the list of worker endpoints available for processing.
    /// </summary>
    public List<string> Endpoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum number of attempts to reach a worker.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
}
