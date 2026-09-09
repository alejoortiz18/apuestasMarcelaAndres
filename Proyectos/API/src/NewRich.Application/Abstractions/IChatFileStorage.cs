namespace NewRich.Application.Abstractions;

public interface IChatFileStorage
{
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
