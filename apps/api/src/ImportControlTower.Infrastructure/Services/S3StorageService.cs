using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ImportControlTower.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImportControlTower.Infrastructure.Services;

public class S3StorageService : IObjectStorageService
{
    private readonly IAmazonS3? _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3StorageService> _logger;
    private readonly bool _useLocalStorage;
    private readonly string _localStoragePath;

    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _logger = logger;
        _bucketName = configuration["STORAGE_BUCKET"] ?? "import-control-tower-documents";
        _localStoragePath = Path.Combine(Path.GetTempPath(), "ict-storage");

        var provider = configuration["STORAGE_PROVIDER"] ?? "Auto";
        var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        var coolifyUrl = configuration["COOLIFY_URL"];
        var coolifyAppId = configuration["COOLIFY_APP_ID"];

        if (string.Equals(provider, "LocalTest", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(coolifyUrl) ||
                !string.IsNullOrEmpty(coolifyAppId))
            {
                _logger.LogCritical("LocalTest storage provider is forbidden in production or Coolify environment.");
                throw new InvalidOperationException("STORAGE_LOCAL_TEST_FORBIDDEN_IN_PRODUCTION: LocalTest storage provider is strictly forbidden in production or Coolify deployments.");
            }

            _useLocalStorage = true;
            Directory.CreateDirectory(_localStoragePath);
            _logger.LogInformation("S3StorageService initialized in LocalTest mode at {Path}", _localStoragePath);
            return;
        }

        // Production / Standard S3 Provider Mode
        var endpoint = configuration["STORAGE_ENDPOINT"] ?? "http://minio:9000";
        var accessKey = configuration["STORAGE_ACCESS_KEY"] ?? "minio_admin";
        var secretKey = configuration["STORAGE_SECRET_KEY"] ?? "minio_password123";
        var forcePathStyle = bool.TryParse(configuration["STORAGE_FORCE_PATH_STYLE"], out var fps) ? fps : true;
        var region = configuration["STORAGE_REGION"] ?? "us-east-1";

        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = forcePathStyle,
            AuthenticationRegion = region,
            Timeout = TimeSpan.FromSeconds(3)
        };

        try
        {
            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
            EnsureBucketExistsAsync().GetAwaiter().GetResult();
            _logger.LogInformation("S3StorageService initialized for bucket {Bucket} at {Endpoint}", _bucketName, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize S3 client or connect to bucket {Bucket} at {Endpoint}", _bucketName, endpoint);
            _s3Client = null;
        }
    }

    private async Task EnsureBucketExistsAsync()
    {
        if (_s3Client == null) return;
        bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);
        if (!exists)
        {
            _logger.LogInformation("Creating private S3 bucket {BucketName}...", _bucketName);
            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _bucketName,
                UseClientRegion = true
            });
        }
    }

    public async Task<string> UploadTempObjectAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var tempKey = $"temp/{Guid.NewGuid()}_{SanitizeFileName(fileName)}";

        if (_useLocalStorage)
        {
            var fullPath = Path.Combine(_localStoragePath, tempKey.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            using (var dest = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(dest, cancellationToken);
            }
            return tempKey;
        }

        if (_s3Client == null)
        {
            throw new InvalidOperationException("STORAGE_UNAVAILABLE: Depolama servisine (MinIO/S3) ulaşılamıyor.");
        }

        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = tempKey,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);
            return tempKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 PutObject failed for key {Key}", tempKey);
            throw new InvalidOperationException("STORAGE_UNAVAILABLE: Depolama servisine (MinIO/S3) ulaşılamıyor.", ex);
        }
    }

    public async Task CopyObjectAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            var srcPath = Path.Combine(_localStoragePath, sourceKey.Replace('/', Path.DirectorySeparatorChar));
            var destPath = Path.Combine(_localStoragePath, destinationKey.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            if (File.Exists(srcPath))
            {
                File.Copy(srcPath, destPath, overwrite: true);
            }
            return;
        }

        if (_s3Client == null)
        {
            throw new InvalidOperationException("STORAGE_UNAVAILABLE: Depolama servisine (MinIO/S3) ulaşılamıyor.");
        }

        try
        {
            var request = new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = sourceKey,
                DestinationBucket = _bucketName,
                DestinationKey = destinationKey
            };

            await _s3Client.CopyObjectAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 CopyObject failed from {SourceKey} to {DestinationKey}", sourceKey, destinationKey);
            throw new InvalidOperationException("STORAGE_UNAVAILABLE: Depolama servisine (MinIO/S3) ulaşılamıyor.", ex);
        }
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            var localPath = Path.Combine(_localStoragePath, objectKey.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                try { File.Delete(localPath); } catch { }
            }
            return;
        }

        if (_s3Client != null)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = objectKey
                };
                await _s3Client.DeleteObjectAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete S3 object {ObjectKey}", objectKey);
                throw;
            }
        }
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(string objectKey, string fileName, TimeSpan expiration, bool isInline = false, CancellationToken cancellationToken = default)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var dispositionType = isInline ? "inline" : "attachment";

        if (_useLocalStorage)
        {
            string fakeUrl = $"http://localhost:8080/api/v1/storage/file/{Uri.EscapeDataString(objectKey)}?disposition={dispositionType}&filename={Uri.EscapeDataString(sanitizedFileName)}";
            return Task.FromResult(fakeUrl);
        }

        if (_s3Client == null)
        {
            throw new InvalidOperationException("STORAGE_UNAVAILABLE: Depolama servisine (MinIO/S3) ulaşılamıyor.");
        }

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.Add(expiration),
                ResponseHeaderOverrides = new ResponseHeaderOverrides
                {
                    ContentDisposition = $"{dispositionType}; filename=\"{sanitizedFileName}\"",
                    CacheControl = "no-store, private"
                }
            };

            string url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate S3 presigned URL for {ObjectKey}", objectKey);
            throw new InvalidOperationException("STORAGE_UNAVAILABLE: Depolama servisine (MinIO/S3) ulaşılamıyor.", ex);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "file";
        var safe = Path.GetFileName(fileName);
        safe = safe.Replace("\"", "").Replace("'", "").Replace("\r", "").Replace("\n", "").Replace(";", "_");
        return safe;
    }
}
