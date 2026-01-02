using Grpc.Core;
using Grpc.Net.Client;
using Ocr.Api.Models;
using Ocr.Grpc;
using Polly;
using Polly.Timeout;

namespace Ocr.Api.Clients;

/// <summary>
/// gRPC client wrapper for communicating with PaddleOCR workers.
/// </summary>
public class PaddleOcrClient
{
    private readonly AsyncPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaddleOcrClient"/> class.
    /// </summary>
    public PaddleOcrClient()
    {
        var retry = Policy
            .Handle<RpcException>(ex => ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.DeadlineExceeded)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(30), TimeoutStrategy.Optimistic);

        _policy = Policy.WrapAsync(retry, timeout);
    }

    /// <summary>
    /// Calls a PaddleOCR worker to extract text from a file.
    /// </summary>
    /// <param name="endpoint">The worker endpoint to call.</param>
    /// <param name="filePath">Path to the file to process.</param>
    /// <param name="requestId">Identifier for correlating the request.</param>
    /// <param name="language">Language code for OCR processing.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    /// <returns>Structured OCR result describing success, text and metadata.</returns>
    public async Task<ExtractTextResult> ExtractTextAsync(string endpoint, string filePath, string requestId, string language, CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async ct =>
        {
            using var channel = GrpcChannel.ForAddress(endpoint);
            var client = new OcrWorker.OcrWorkerClient(channel);

            var request = new ExtractTextRequest
            {
                RequestId = requestId,
                FilePath = filePath,
                Language = language,
                UseAngleCls = true
            };

            var start = DateTime.UtcNow;
            try
            {
                var response = await client.ExtractTextAsync(request, cancellationToken: ct);
                return new ExtractTextResult
                {
                    Success = response.Ok,
                    Text = response.Text,
                    Error = response.Error,
                    ElapsedMs = (long)(DateTime.UtcNow - start).TotalMilliseconds,
                    SourceEndpoint = endpoint
                };
            }
            catch (RpcException ex)
            {
                return new ExtractTextResult
                {
                    Success = false,
                    Error = ex.Status.Detail,
                    ElapsedMs = (long)(DateTime.UtcNow - start).TotalMilliseconds,
                    SourceEndpoint = endpoint
                };
            }
        }, cancellationToken);
    }
}
