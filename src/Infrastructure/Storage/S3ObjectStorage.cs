using Amazon.S3;
using Amazon.S3.Model;
using Application.Storage;

namespace Infrastructure.Storage;

public sealed class S3ObjectStorage(IAmazonS3 client, ObjectStorageOptions options) : IObjectStorage
{
    public async Task<StoredObject> PutAsync(ObjectWriteRequest request, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(request.Key);
        try
        {
            var response = await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket(request.Visibility),
                Key = request.Key,
                InputStream = request.Content,
                AutoCloseStream = false,
                ContentType = request.ContentType,
                Headers = { ContentLength = request.SizeBytes, CacheControl = request.CacheControl },
                Metadata = { ["sha256"] = request.Checksum },
            }, cancellationToken);
            return new StoredObject(request.Key, request.ContentType, request.SizeBytes, request.Checksum,
                response.ETag, DateTimeOffset.UtcNow, request.Visibility);
        }
        catch (AmazonS3Exception exception)
        {
            throw new ObjectStorageUnavailableException("Object storage write failed.", exception);
        }
    }

    public async Task<StoredObject?> HeadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(key);
        try
        {
            var response = await client.GetObjectMetadataAsync(Bucket(visibility), key, cancellationToken);
            return new StoredObject(key, response.Headers.ContentType, response.ContentLength,
                response.Metadata["x-amz-meta-sha256"] ?? string.Empty, response.ETag,
                response.LastModified, visibility);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (AmazonS3Exception exception)
        {
            throw new ObjectStorageUnavailableException("Object storage metadata read failed.", exception);
        }
    }

    public async Task<ObjectReadResult?> OpenReadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(key);
        try
        {
            var response = await client.GetObjectAsync(Bucket(visibility), key, cancellationToken);
            var metadata = new StoredObject(key, response.Headers.ContentType, response.ContentLength,
                response.Metadata["x-amz-meta-sha256"] ?? string.Empty, response.ETag,
                response.LastModified, visibility);
            return new ObjectReadResult(response.ResponseStream, metadata);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (AmazonS3Exception exception)
        {
            throw new ObjectStorageUnavailableException("Object storage read failed.", exception);
        }
    }

    public async Task DeleteAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(key);
        try
        {
            await client.DeleteObjectAsync(Bucket(visibility), key, cancellationToken);
        }
        catch (AmazonS3Exception exception)
        {
            throw new ObjectStorageUnavailableException("Object storage delete failed.", exception);
        }
    }

    public Uri GetPublicUrl(string key)
    {
        ObjectKeys.Validate(key);
        return new Uri($"{options.PublicBaseUrl.TrimEnd('/')}/{string.Join('/', key.Split('/').Select(Uri.EscapeDataString))}");
    }

    public Task<Uri> CreatePrivateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(key);
        var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.PrivateBucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime),
        });
        return Task.FromResult(new Uri(url));
    }

    private string Bucket(ObjectVisibility visibility) =>
        visibility == ObjectVisibility.Public ? options.PublicBucket : options.PrivateBucket;
}
