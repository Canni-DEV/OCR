using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;

namespace Ocr.Api.Services;

/// <summary>
/// Enforces Azure usage limits backed by SQL storage.
/// </summary>
public class AzureUsageLimiter
{
    private readonly DbOptions _dbOptions;
    private readonly AzureOptions _azureOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureUsageLimiter"/> class.
    /// </summary>
    /// <param name="dbOptions">Database configuration options.</param>
    /// <param name="azureOptions">Azure usage configuration.</param>
    public AzureUsageLimiter(IOptions<DbOptions> dbOptions, IOptions<AzureOptions> azureOptions)
    {
        _dbOptions = dbOptions.Value;
        _azureOptions = azureOptions.Value;
    }

    /// <summary>
    /// Attempts to consume a single Azure usage unit for the current billing period.
    /// </summary>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    /// <returns><c>true</c> if usage remains under the configured limit; otherwise, <c>false</c>.</returns>
    public async Task<bool> TryConsumeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var (periodStart, periodEnd) = GetCurrentPeriod(now, _azureOptions.ResetDay);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
MERGE AzureUsage WITH (HOLDLOCK) AS target
USING (SELECT @start AS PeriodStartUtc, @end AS PeriodEndUtc) AS source
ON target.PeriodStartUtc = source.PeriodStartUtc AND target.PeriodEndUtc = source.PeriodEndUtc
WHEN MATCHED THEN UPDATE SET UsedCount = UsedCount + 1
WHEN NOT MATCHED THEN INSERT (PeriodStartUtc, PeriodEndUtc, UsedCount) VALUES (source.PeriodStartUtc, source.PeriodEndUtc, 1)
OUTPUT inserted.UsedCount;";

        command.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime2) { Value = periodStart.UtcDateTime });
        command.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime2) { Value = periodEnd.UtcDateTime });

        var usedCount = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return usedCount <= GetHardLimit();
    }

    /// <summary>
    /// Computes the start and end dates for the current Azure billing period.
    /// </summary>
    /// <param name="now">Current timestamp.</param>
    /// <param name="resetDay">Configured reset day of the month.</param>
    /// <returns>A tuple containing the start and end of the period.</returns>
    public (DateTimeOffset start, DateTimeOffset end) GetCurrentPeriod(DateTimeOffset now, int resetDay)
    {
        var start = new DateTimeOffset(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));
        if (resetDay > 1)
        {
            start = new DateTimeOffset(new DateTime(now.Year, now.Month, Math.Min(resetDay, DateTime.DaysInMonth(now.Year, now.Month)), 0, 0, 0, DateTimeKind.Utc));
        }

        var end = start.AddMonths(1);
        return (start, end);
    }

    private int GetHardLimit() => _azureOptions.HardLimit > 0 ? _azureOptions.HardLimit : 50000;
}
