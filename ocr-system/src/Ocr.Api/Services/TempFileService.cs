using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;

namespace Ocr.Api.Services;

/// <summary>
/// Provides utilities for working with temporary files created during OCR requests.
/// </summary>
public class TempFileService
{
    private readonly IFileSystem _fileSystem;
    private readonly TempStorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use.</param>
    /// <param name="options">The temporary storage configuration.</param>
    public TempFileService(IFileSystem fileSystem, IOptions<TempStorageOptions> options)
    {
        _fileSystem = fileSystem;
        _options = options.Value;
    }

    /// <summary>
    /// Saves the uploaded file as a temporary file identified by the request id.
    /// </summary>
    /// <param name="file">The incoming form file.</param>
    /// <param name="requestId">The request identifier used to build the file name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The full path to the saved temporary file.</returns>
    public async Task<string> SaveTempFileAsync(IFormFile file, string requestId, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(file.FileName);
        var targetPath = _fileSystem.Path.Combine(_options.Root, $"{requestId}{ext}");

        await using var stream = _fileSystem.File.Create(targetPath);
        await file.CopyToAsync(stream, cancellationToken);
        return targetPath;
    }

    /// <summary>
    /// Attempts to delete the provided temporary file path.
    /// </summary>
    /// <param name="path">The full path to delete.</param>
    public Task DeleteAsync(string path)
    {
        try
        {
            if (_fileSystem.File.Exists(path))
            {
                _fileSystem.File.Delete(path);
            }
        }
        catch
        {
            // swallow cleanup errors
        }

        return Task.CompletedTask;
    }
}
