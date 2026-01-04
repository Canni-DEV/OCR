using Ocr.Api.Models;

namespace Ocr.Api.Repositories;

/// <summary>
/// Abstraction for persisting OCR audit records.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Persists the provided audit record.
    /// </summary>
    /// <param name="record">Audit entry to store.</param>
    /// <param name="cancellationToken">Token to observe cancellation.</param>
    Task InsertAsync(AuditRecord record, CancellationToken cancellationToken);
}
