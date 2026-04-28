using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DWIMS.Service.Storage;
using Microsoft.Extensions.Options;

namespace DWIMS.Service.Services;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _client;
    private readonly StorageOptions _options;

    public AzureBlobStorageService(BlobServiceClient client, IOptions<StorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(_options.BucketName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(content, new BlobHttpHeaders() { ContentType = contentType }, cancellationToken: cancellationToken);
        
        return blob.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = new BlobClient(new Uri(storageKey));
        var response = await blob.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToStream();
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = new BlobClient(new Uri(storageKey));
        await blob.DeleteAsync(cancellationToken: cancellationToken);
    }
}