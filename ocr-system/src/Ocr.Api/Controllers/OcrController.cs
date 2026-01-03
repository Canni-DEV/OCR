using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Ocr.Api.Clients;
using Ocr.Api.Models;
using Ocr.Api.Options;
using Ocr.Api.Repositories;
using Ocr.Api.Services;
using Ocr.Api.Processing;

namespace Ocr.Api.Controllers;

/// <summary>
/// Handles OCR requests routed through the API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OcrController : ControllerBase
{
    private static readonly string[] AllowedMimeTypes = { "image/png", "image/jpeg", "application/pdf" };

    private readonly TempFileService _tempFileService;
    private readonly WorkerPoolService _workerPoolService;
    private readonly PaddleOcrClient _paddleClient;
    private readonly AzureReadClient _azureClient;
    private readonly TextPostProcessor _postProcessor;
    private readonly AzureUsageLimiter _usageLimiter;
    private readonly AuditRepository _auditRepository;
    private readonly WorkerOptions _workerOptions;
    private readonly OcrOptions _ocrOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrController"/> class.
    /// </summary>
    /// <param name="tempFileService">Service for storing temporary uploaded files.</param>
    /// <param name="workerPoolService">Service that manages PaddleOCR workers.</param>
    /// <param name="paddleClient">gRPC client for PaddleOCR workers.</param>
    /// <param name="azureClient">HTTP client for Azure Read fallback.</param>
    /// <param name="postProcessor">Service to normalize OCR results.</param>
    /// <param name="usageLimiter">Azure usage limiter.</param>
    /// <param name="auditRepository">Repository for audit trail persistence.</param>
    /// <param name="workerOptions">Worker configuration options.</param>
    /// <param name="ocrOptions">OCR configuration options.</param>
    public OcrController(
        TempFileService tempFileService,
        WorkerPoolService workerPoolService,
        PaddleOcrClient paddleClient,
        AzureReadClient azureClient,
        TextPostProcessor postProcessor,
        AzureUsageLimiter usageLimiter,
        AuditRepository auditRepository,
        IOptions<WorkerOptions> workerOptions,
        IOptions<OcrOptions> ocrOptions)
    {
        _tempFileService = tempFileService;
        _workerPoolService = workerPoolService;
        _paddleClient = paddleClient;
        _azureClient = azureClient;
        _postProcessor = postProcessor;
        _usageLimiter = usageLimiter;
        _auditRepository = auditRepository;
        _workerOptions = workerOptions.Value;
        _ocrOptions = ocrOptions.Value;
    }

    /// <summary>
    /// Processes an uploaded file through OCR and returns structured text results.
    /// </summary>
    /// <param name="request">Request containing the file to process.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    /// <returns>The OCR response or a problem description.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    [ProducesResponseType(typeof(OcrResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Post([FromForm] OcrRequest request, CancellationToken cancellationToken)
    {
        var file = request?.File;

        if (file is null)
        {
            return BadRequest(Problem("File is required"));
        }

        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            return BadRequest(Problem("Unsupported MIME type"));
        }

        if (file.Length == 0)
        {
            return BadRequest(Problem("Empty file"));
        }

        var requestId = Guid.NewGuid().ToString("N");
        var tempPath = string.Empty;
        var stopwatch = Stopwatch.StartNew();
        string? correlationId = HttpContext.Items["CorrelationId"]?.ToString();

        try
        {
            tempPath = await _tempFileService.SaveTempFileAsync(file, requestId, cancellationToken);

            ExtractTextResult? paddleResult = null;
            var attempt = 0;
            var endpointsTried = new List<string>();

            while (attempt < _workerOptions.MaxAttempts)
            {
                attempt++;
                await using var lease = await _workerPoolService.AcquireAsync(_workerOptions.AcquireTimeoutSeconds, cancellationToken);
                if (lease is null)
                {
                    break;
                }

                endpointsTried.Add(lease.Endpoint);

                paddleResult = await _paddleClient.ExtractTextAsync(lease.Endpoint, tempPath, requestId, _ocrOptions.DefaultLanguage, cancellationToken);

                if (paddleResult.Success)
                {
                    break;
                }
            }

            if (paddleResult is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, Problem("Failed to reach worker"));
            }

            var resultText = paddleResult.Text ?? string.Empty;
            var source = "paddle";

            if (!paddleResult.Success)
            {
                if (await _usageLimiter.TryConsumeAsync(cancellationToken))
                {
                    var azureResult = await _azureClient.ReadTextAsync(tempPath, requestId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(azureResult))
                    {
                        resultText = azureResult;
                        source = "azure";
                    }
                }
            }

            var processed = await _postProcessor.ProcessAsync(resultText, cancellationToken);

            await _auditRepository.InsertAsync(new AuditRecord
            {
                RequestId = requestId,
                FileName = file.FileName,
                Length = file.Length,
                Source = source,
                TimestampUtc = DateTime.UtcNow,
                EndpointsTried = endpointsTried
            }, cancellationToken);

            return Ok(new OcrResponse
            {
                RequestId = requestId,
                Text = resultText,
                Source = source,
                Processed = processed,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                CorrelationId = correlationId
            });
        }
        catch (OperationCanceledException)
        {
            return Problem("The request was canceled", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        finally
        {
            stopwatch.Stop();
            if (!string.IsNullOrWhiteSpace(tempPath))
            {
                await _tempFileService.DeleteAsync(tempPath);
            }
        }
    }
}
