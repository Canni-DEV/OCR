using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;

namespace Ocr.Api.Services;

public class RateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, Counter> Counters = new();
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;

    public RateLimitMiddleware(RequestDelegate next, IOptions<RateLimitOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTimeOffset.UtcNow;

        var counter = Counters.AddOrUpdate(
            key,
            _ => new Counter { Count = 1, WindowEnd = now.AddSeconds(_options.WindowSeconds) },
            (_, existing) =>
            {
                if (now > existing.WindowEnd)
                {
                    return new Counter { Count = 1, WindowEnd = now.AddSeconds(_options.WindowSeconds) };
                }

                existing.Count += 1;
                return existing;
            });

        if (counter.Count > _options.PermitLimit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }

    private sealed class Counter
    {
        public int Count { get; set; }

        public DateTimeOffset WindowEnd { get; set; }
    }
}
