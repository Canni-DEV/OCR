using System.Data.SqlClient;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocr.Api.Models;
using Ocr.Api.Options;

namespace Ocr.Api.Repositories;

public class AuditRepository
{
    private readonly DbOptions _options;
    private readonly ILogger<AuditRepository> _logger;

    public AuditRepository(IOptions<DbOptions> options, ILogger<AuditRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

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
