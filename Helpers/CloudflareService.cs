using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.Runtime;

namespace BabaiBazaar.API.Helpers;

public interface ICloudflareService
{
    Task<UploadResult> UploadImageAsync(IFormFile file, string folder);
    Task<UploadResult> UploadVideoAsync(IFormFile file, string folder);
    Task<UploadResult> UploadFileAsync(IFormFile file, string folder);
    Task<bool> DeleteFileAsync(string fileKey);
    string GetPublicUrl(string fileKey);
}

public record UploadResult(bool Success, string Url, string FileKey, string? Error = null);

public class CloudflareService(IConfiguration config, ILogger<CloudflareService> logger) : ICloudflareService
{
    readonly string _accessKey = config["CloudflareR2:AccessKeyId"]!;
    readonly string _secretKey = config["CloudflareR2:SecretAccessKey"]!;
    readonly string _bucket = config["CloudflareR2:BucketName"]!;
    readonly string _publicUrl = config["CloudflareR2:PublicUrl"]!;
    readonly string _endpoint = config["CloudflareR2:Endpoint"]!;

    static readonly string[] ImageExts = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"];
    static readonly string[] VideoExts = [".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v"];

    AmazonS3Client CreateClient() => new(
        new BasicAWSCredentials(_accessKey, _secretKey),
        new AmazonS3Config
        {
            ServiceURL = _endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4",
            // Note: UseChunkedEncoding is set per-request on PutObjectRequest,
            // not on AmazonS3Config in newer SDK versions
        }
    );

    public Task<UploadResult> UploadImageAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!ImageExts.Contains(ext))
            return Task.FromResult(new UploadResult(false, "", "", $"Invalid image type: {ext}"));
        return UploadAsync(file, folder);
    }

    public Task<UploadResult> UploadVideoAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!VideoExts.Contains(ext))
            return Task.FromResult(new UploadResult(false, "", "", $"Invalid video type: {ext}"));
        return UploadAsync(file, folder, isLargeFile: true);
    }

    public Task<UploadResult> UploadFileAsync(IFormFile file, string folder)
        => UploadAsync(file, folder);

    async Task<UploadResult> UploadAsync(IFormFile file, string folder, bool isLargeFile = false)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        var key = $"{folder.TrimEnd('/')}/{Guid.NewGuid():N}{ext}";

        try
        {
            using var client = CreateClient();

            // Videos > 100MB: use multipart upload (bypasses RAM loading)
            if (isLargeFile && file.Length > 100 * 1024 * 1024)
            {
                logger.LogInformation("Starting multipart upload for {Name} ({Size}MB)",
                    file.FileName, file.Length / 1024 / 1024);

                var transfer = new TransferUtility(client);
                var uploadReq = new TransferUtilityUploadRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    ContentType = file.ContentType ?? "video/mp4",
                    CannedACL = S3CannedACL.PublicRead,
                    InputStream = file.OpenReadStream(),
                    // 50 MB per part — R2 supports up to 10,000 parts
                    PartSize = 50 * 1024 * 1024,
                    AutoCloseStream = true,
                };

                uploadReq.UploadProgressEvent += (_, e) =>
                    logger.LogInformation("Upload progress: {Pct}% ({MB}MB of {TotalMB}MB)",
                        e.PercentDone, e.TransferredBytes / 1024 / 1024, e.TotalBytes / 1024 / 1024);

                await transfer.UploadAsync(uploadReq);
            }
            else
            {
                // Small/medium files: read into MemoryStream (R2 needs Content-Length)
                using var ms = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
                await file.CopyToAsync(ms);
                ms.Position = 0;

                var req = new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    InputStream = ms,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    CannedACL = S3CannedACL.PublicRead,
                    UseChunkEncoding = false,
                    DisableMD5Stream = true,
                    DisableDefaultChecksumValidation = true,
                    Headers = { ContentLength = ms.Length },
                };
                await client.PutObjectAsync(req);
            }

            var url = $"{_publicUrl.TrimEnd('/')}/{key}";
            logger.LogInformation("R2 upload complete: {Key}", key);
            return new UploadResult(true, url, key);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "R2 S3 error [{Code}] for {Name}", ex.ErrorCode, file.FileName);
            return new UploadResult(false, "", "", $"{ex.ErrorCode}: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "R2 upload failed for {Name}", file.FileName);
            return new UploadResult(false, "", "", ex.Message);
        }
    }

    public async Task<bool> DeleteFileAsync(string fileKey)
    {
        try
        {
            using var client = CreateClient();
            await client.DeleteObjectAsync(_bucket, fileKey);
            logger.LogInformation("Deleted: {Key}", fileKey);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete failed: {Key}", fileKey);
            return false;
        }
    }

    public string GetPublicUrl(string fileKey)
        => $"{_publicUrl.TrimEnd('/')}/{fileKey}";
}