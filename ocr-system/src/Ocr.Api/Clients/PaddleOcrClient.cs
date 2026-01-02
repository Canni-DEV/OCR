using Grpc.Core;
using Grpc.Net.Client;
using Ocr.Api.Models;
using Ocr.Grpc;
using Polly;
using Polly.Timeout;

namespace Ocr.Api.Clients;

public class PaddleOcrClient
{
    private readonly AsyncPolicy _policy;

    public PaddleOcrClient()
    {
        var retry = Policy
            .Handle<RpcException>(ex => ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.DeadlineExceeded)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(30), TimeoutStrategy.Optimistic);

        _policy = Policy.WrapAsync(retry, timeout);
    }

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
