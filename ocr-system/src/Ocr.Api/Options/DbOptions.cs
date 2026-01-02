namespace Ocr.Api.Options;

/// <summary>
/// Database connection configuration.
/// </summary>
public class DbOptions
{
    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
