namespace SMPP.Application.Abstractions;

/// <summary>Saves an uploaded file and returns a relative path suitable for serving from wwwroot.</summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string originalFileName, string subFolder, CancellationToken ct = default);
}
