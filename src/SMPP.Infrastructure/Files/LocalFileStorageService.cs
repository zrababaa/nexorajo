using Microsoft.Extensions.Options;
using SMPP.Application.Abstractions;

namespace SMPP.Infrastructure.Files;

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;

    public LocalFileStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string subFolder, CancellationToken ct = default)
    {
        var safeExtension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var relativePath = Path.Combine("uploads", subFolder, fileName).Replace('\\', '/');
        var fullPath = Path.Combine(_options.RootPath, "uploads", subFolder, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return relativePath;
    }
}
