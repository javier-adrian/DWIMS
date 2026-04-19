using Amazon.S3;
using Amazon.S3.Model;
using DWIMS.Service.Storage;
using Microsoft.Extensions.Options;

namespace DWIMS.Service.Services;

public sealed class StorageService(
    IAmazonS3 s3, 
    IOptions<StorageOptions> storageOptions) : IStorageService
{
    private readonly StorageOptions _storageOptions = storageOptions.Value;
    
    public async Task<string> UploadAsync(
        Stream content, 
        string fileName, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        var key = $"{_storageOptions.TemplatePrefix}/{Guid.NewGuid()}_{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _storageOptions.BucketName,
            Key = key,
            ContentType = contentType,
            InputStream = content,
            AutoCloseStream = false
        };
        
        await s3.PutObjectAsync(request, cancellationToken);
        
        return key;
    }

    public async Task<Stream> DownloadAsync(
        string storageKey, 
        CancellationToken cancellationToken = default)
    {
        var response = await s3.GetObjectAsync(
            _storageOptions.BucketName,
            storageKey,
            cancellationToken
        );
        
        var ms = new MemoryStream();
        
        await response.ResponseStream.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(
        string storageKey, 
        CancellationToken cancellationToken = default)
    {
        await s3.DeleteObjectAsync(
            _storageOptions.BucketName,
            storageKey,
            cancellationToken
        );
    }
}