using Microsoft.Extensions.Configuration;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Storage;

public sealed class LocalChatFileStorage : IChatFileStorage
{
    private readonly string _root;

    public LocalChatFileStorage(IConfiguration configuration)
    {
        _root = configuration["Storage:Chat"] ?? Path.Combine(AppContext.BaseDirectory, "chat-files");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var relative = $"{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        var full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var file = File.Create(full);
        await content.CopyToAsync(file, cancellationToken);
        return relative;
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var full = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = File.OpenRead(full);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        var full = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }
}
