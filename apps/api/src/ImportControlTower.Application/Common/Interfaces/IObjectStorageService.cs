using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImportControlTower.Application.Common.Interfaces;

public interface IObjectStorageService
{
    Task<string> UploadTempObjectAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task CopyObjectAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<string> GeneratePresignedDownloadUrlAsync(string objectKey, string fileName, TimeSpan expiration, bool isInline = false, CancellationToken cancellationToken = default);
}
