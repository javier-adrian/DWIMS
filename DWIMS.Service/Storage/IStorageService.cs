namespace DWIMS.Service.Storage;

public interface IStorageService
{
    Task<string> UploadAsync(
        Stream stream, 
        string fileName, 
        string contentType, 
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string storageKey, 
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        string storageKey, 
        CancellationToken cancellationToken = default);
}