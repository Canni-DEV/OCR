using System.Data.SqlClient;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocr.Api.Models;
using Ocr.Api.Options;

namespace Ocr.Api.Repositories;

/// <summary>
/// SQL Server-backed implementation of <see cref="IAuditRepository"/>.
/// </summary>
public class SqlAuditRepository : IAuditRepository
{
    private readonly DbOptions _options;
    private readonly ILogger<SqlAuditRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlAuditRepository"/> class.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="logger">Logger for capturing persistence issues.</param>
    public SqlAuditRepository(IOptions<DbOptions> options, ILogger<SqlAuditRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InsertAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO OcrAudit (RequestId, FileName, Length, Source, CreatedUtc, Endpoints)
VALUES (@id, @file, @length, @source, @createdUtc, @endpoints);";

            command.Parameters.AddWithValue("@id", record.RequestId);
            command.Parameters.AddWithValue("@file", record.FileName);
            command.Parameters.AddWithValue("@length", record.Length);
            command.Parameters.AddWithValue("@source", record.Source);
            command.Parameters.AddWithValue("@createdUtc", record.TimestampUtc);
            command.Parameters.AddWithValue("@endpoints", JsonSerializer.Serialize(record.EndpointsTried));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist audit record for {RequestId}", record.RequestId);
        }
    }
}
