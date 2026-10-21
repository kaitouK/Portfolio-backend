using MyPortfolio.Service.Interface;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace MyPortfolio.Service
{
    /// <summary>
    /// Artwork 和 Journal 共用的唯一 Azure Blob 出入口：<br/>
    /// UploadAsync(stream, blobName, contentType) 回傳 URL<br/>
    /// DeleteAsync(blobName) 靜默容錯（刪失敗記 log 回 false，不炸呼叫方）。<br/>
    /// CreateIfNotExists 延後到每個 process 第一次上傳時才跑一次，避免拖慢冷啟動後的第一個請求。<br/>
    /// </summary>
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobService> _logger;
        private volatile bool _containerEnsured;
        public BlobService(IConfiguration configuration, ILogger<BlobService> logger)
        {
            _logger = logger;
            var connectionString = configuration.GetSection("BlobStorage")["ConnectionString"];
            var containerName = configuration.GetSection("BlobStorage")["ContainerName"];
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
            {
                _logger.LogCritical("Blob Storage 配置遺失！請檢察 ConnectionString與ContainerName。");
                throw new InvalidOperationException("Blob Storage 配置未正確載入");
            }
            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }
        public async Task<string> UploadAsync(Stream stream, string blobName, string contentType)
        {
            // 重複呼叫是安全的，併發時多跑一次也無害
            if (!_containerEnsured)
            {
                await _containerClient.CreateIfNotExistsAsync();
                _containerEnsured = true;
            }
            var blobClient = _containerClient.GetBlobClient(blobName);
            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };
            stream.Position = 0;
            await blobClient.UploadAsync(stream, options);
            return blobClient.Uri.ToString();

        }
        // 找不到 blob 時會丟 RequestFailedException，由呼叫方處理
        public async Task<Stream> DownloadAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToStream();
        }
        public async Task<bool> DeleteAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName)) return false;
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理 Azure Blob 失敗: {BlobName}", blobName);
                return false;
            }
        }

    }
}