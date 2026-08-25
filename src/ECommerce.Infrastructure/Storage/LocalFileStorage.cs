using ECommerce.Application.Abstractions;

namespace ECommerce.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorage(string basePath, string baseUrl = "/uploads")
    {
        _basePath = basePath;
        _baseUrl = baseUrl;

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_basePath, storageKey);

        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, storageKey);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, storageKey);

        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public string GetUrl(string storageKey)
    {
        return $"{_baseUrl}/{storageKey}";
    }
}
