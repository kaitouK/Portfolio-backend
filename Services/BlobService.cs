using MyPortfolio.Service.Interface;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
namespace MyPortfolio.Service
{
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobService> _logger;
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
            _containerClient.CreateIfNotExists();
        }
        public async Task<string> UploadAsync(Stream stream, string blobName, string contentType)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };
            stream.Position = 0;
            await blobClient.UploadAsync(stream, options);
            return blobClient.Uri.ToString();

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