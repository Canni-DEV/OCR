using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using Ocr.Api.Options;

namespace Ocr.Api.Services;

public class TempFileService
{
    private readonly IFileSystem _fileSystem;
    private readonly TempStorageOptions _options;

    public TempFileService(IFileSystem fileSystem, IOptions<TempStorageOptions> options)
    {
        _fileSystem = fileSystem;
        _options = options.Value;
    }

    public async Task<string> SaveTempFileAsync(IFormFile file, string requestId, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(file.FileName);
        var targetPath = _fileSystem.Path.Combine(_options.Root, $"{requestId}{ext}");

        await using var stream = _fileSystem.FileStream.Create(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);
        return targetPath;
    }

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
