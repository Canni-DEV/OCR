using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocr.Api.Models;
using Ocr.Api.Options;

namespace Ocr.Api.Repositories;

/// <summary>
/// LiteDB-backed implementation of <see cref="IAuditRepository"/> for local and portable setups.
/// </summary>
public class LiteDbAuditRepository : IAuditRepository, IDisposable
{
    private readonly DbOptions _options;
    private readonly ILogger<LiteDbAuditRepository> _logger;
    private readonly Lazy<LiteDatabase> _database;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteDbAuditRepository"/> class.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="logger">Logger for capturing persistence issues.</param>
    public LiteDbAuditRepository(IOptions<DbOptions> options, ILogger<LiteDbAuditRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
        _database = new Lazy<LiteDatabase>(() => new LiteDatabase(new ConnectionString
        {
            Filename = _options.LiteDbPath,
            Connection = ConnectionType.Shared
        }));
    }

    /// <inheritdoc />
    public Task InsertAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var collection = _database.Value.GetCollection("ocr_audit");
            collection.EnsureIndex("RequestId");

            var document = new BsonDocument
            {
                ["RequestId"] = record.RequestId,
                ["FileName"] = record.FileName,
                ["Length"] = record.Length,
                ["Source"] = record.Source,
                ["CreatedUtc"] = record.TimestampUtc,
                ["Endpoints"] = new BsonArray(record.EndpointsTried.Select(endpoint => new BsonValue(endpoint)))
            };

            collection.Insert(document);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist audit record for {RequestId} to LiteDB", record.RequestId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases unmanaged resources held by the repository.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_database.IsValueCreated)
        {
            _database.Value.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
