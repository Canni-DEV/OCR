using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;
using Polly;
using Polly.Timeout;

namespace Ocr.Api.Clients;

public class AzureReadClient
{
    private readonly HttpClient _httpClient;
    private readonly AzureOptions _options;
    private readonly AsyncPolicy<string?> _policy;

    public AzureReadClient(HttpClient httpClient, IOptions<AzureOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        var retry = Policy
            .Handle<HttpRequestException>()
            .OrResult<string?>(static r => r is null)
            .WaitAndRetryAsync(_options.RetryCount, attempt => TimeSpan.FromMilliseconds(500 * attempt));

        var timeout = Policy.TimeoutAsync<string?>(TimeSpan.FromSeconds(_options.TimeoutSeconds), TimeoutStrategy.Optimistic);

        _policy = Policy.WrapAsync(retry, timeout);
    }

    public async Task<string?> ReadTextAsync(string filePath, string requestId, CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async ct =>
        {
            await using var stream = File.OpenRead(filePath);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = content
            };
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
            request.Headers.Add("X-Request-Id", requestId);

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(ct);
            var text = ExtractReadResult(payload);
            return text;
        }, cancellationToken);
    }

    private static string? ExtractReadResult(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var lines = document
                .RootElement
                .GetProperty("analyzeResult")
                .GetProperty("readResults")
                .EnumerateArray()
                .SelectMany(page => page.GetProperty("lines").EnumerateArray())
                .Select(line => line.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();

            return string.Join(Environment.NewLine, lines);
        }
        catch
        {
            return payload;
        }
    }
}
