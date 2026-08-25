namespace ECommerce.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default);
    string GetUrl(string storageKey);
}
