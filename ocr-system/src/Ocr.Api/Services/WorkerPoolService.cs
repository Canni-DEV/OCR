using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;

namespace Ocr.Api.Services;

public class WorkerPoolService
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<string> _endpoints;

    public WorkerPoolService(IOptions<WorkerOptions> options)
    {
        _endpoints = new ConcurrentQueue<string>(options.Value.Endpoints);
        var capacity = Math.Max(1, options.Value.Endpoints.Count);
        _semaphore = new SemaphoreSlim(capacity, capacity);
    }

    public async Task<WorkerLease?> AcquireAsync(int timeoutSeconds, CancellationToken cancellationToken)
    {
        var acquired = await _semaphore.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);
        if (!acquired)
        {
            return null;
        }

        if (_endpoints.TryDequeue(out var endpoint))
        {
            return new WorkerLease(this, endpoint);
        }

        _semaphore.Release();
        return null;
    }

    public Task<string?> TryGetAvailableEndpointAsync()
    {
        if (_endpoints.TryDequeue(out var endpoint))
        {
            return Task.FromResult<string?>(endpoint);
        }

        return Task.FromResult<string?>(null);
    }

    internal void ReleaseEndpoint(string endpoint)
    {
        _endpoints.Enqueue(endpoint);
        _semaphore.Release();
    }

    public sealed class WorkerLease : IAsyncDisposable
    {
        private readonly WorkerPoolService _pool;
        private bool _disposed;

        public WorkerLease(WorkerPoolService pool, string endpoint)
        {
            _pool = pool;
            Endpoint = endpoint;
        }

        public string Endpoint { get; }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _pool.ReleaseEndpoint(Endpoint);
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
