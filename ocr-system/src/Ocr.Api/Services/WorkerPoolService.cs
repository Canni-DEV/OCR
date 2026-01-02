using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;

namespace Ocr.Api.Services;

/// <summary>
/// Coordinates access to a pool of PaddleOCR worker endpoints.
/// </summary>
public class WorkerPoolService
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<string> _endpoints;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkerPoolService"/> class.
    /// </summary>
    /// <param name="options">Worker configuration options.</param>
    public WorkerPoolService(IOptions<WorkerOptions> options)
    {
        _endpoints = new ConcurrentQueue<string>(options.Value.Endpoints);
        var capacity = Math.Max(1, options.Value.Endpoints.Count);
        _semaphore = new SemaphoreSlim(capacity, capacity);
    }

    /// <summary>
    /// Attempts to acquire an available worker endpoint within the specified timeout.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum time to wait for a worker.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    /// <returns>A disposable lease containing the endpoint, or null if none available.</returns>
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

    /// <summary>
    /// Attempts to return an available worker endpoint without waiting.
    /// </summary>
    /// <returns>The endpoint string if available; otherwise, null.</returns>
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

    /// <summary>
    /// Represents a temporary claim on a worker endpoint.
    /// </summary>
    public sealed class WorkerLease : IAsyncDisposable
    {
        private readonly WorkerPoolService _pool;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerLease"/> class.
        /// </summary>
        /// <param name="pool">Parent pool that created the lease.</param>
        /// <param name="endpoint">Endpoint reserved by the lease.</param>
        public WorkerLease(WorkerPoolService pool, string endpoint)
        {
            _pool = pool;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Gets the endpoint associated with the lease.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Releases the endpoint back to the pool.
        /// </summary>
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
