namespace Ocr.Api.Options;

/// <summary>
/// Database connection configuration.
/// </summary>
public class DbOptions
{
    /// <summary>
    /// Gets or sets the database provider to use for audit persistence.
    /// Supported values: <c>SqlServer</c>, <c>LiteDb</c>.
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the LiteDB database file path when using the LiteDB provider.
    /// </summary>
    public string LiteDbPath { get; set; } = "lite.db";
}
