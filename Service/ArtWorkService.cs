using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Service.Interface;
using MyPortfolio.DTOs;
using MyPortfolio.Model.Entities;
using MyPortfolio.Model;
using SkiaSharp;
using MyPortfolio.Repository;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Reflection.Metadata;
using Serilog;

namespace MyPortfolio.Service
{
    public class ArtworkService : IArtworkService
    {
        private readonly IArtworkRepository _artworkRepository;
        // 定義一個儲存上傳檔案的路徑，這裡使用 wwwroot/uploads 資料夾
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<ArtworkService> _logger;
        public ArtworkService(IArtworkRepository artworkRepository, ILogger<ArtworkService> logger, IConfiguration configuration)
        {
            _artworkRepository = artworkRepository;
            _logger = logger;
            string? connectionString = configuration.GetSection("BlobStorage")["ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Blob Storage 連線字串未正確載入");
                throw new InvalidOperationException("Blob Storage 連線字串未正確載入");
            }

            //初始化 Blob 容器用戶端
            var blobServiceClient = new BlobServiceClient(connectionString);
            string? BlobContainerName = configuration.GetSection("BlobStorage")["ContainerName"];

            if (string.IsNullOrEmpty(BlobContainerName))
            {
                _logger.LogError("Blob Container 名稱 未正確載入");
                throw new InvalidOperationException("Blob Container 名稱 未正確載入");
            }

            _containerClient = blobServiceClient.GetBlobContainerClient(BlobContainerName);
            _containerClient.CreateIfNotExists();

        }

        public async Task<ServiceResult<ArtworkResponse>> SaveArtworkAsync(ArtworkUploadRequest dto)
        {
            // 生成唯一檔名，避免重複 
            var fileGuid = Guid.NewGuid();
            var fileName = $"{fileGuid}.webp";// 原圖使用WebP格式
            var thumbName = $"{fileGuid}_thumb.webp"; // 縮圖使用 WebP 格式
            var blobClient = _containerClient.GetBlobClient(fileName);
            var thumbBlobClient = _containerClient.GetBlobClient(thumbName);
            try
            {
                // 1. 儲存實體檔案
                using (var memoryStream = new MemoryStream())
                {
                    await dto.File.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    using (var original = SKBitmap.Decode(memoryStream))
                    {
                        if (original == null)
                        {
                            return ServiceResult<ArtworkResponse>.Fail("無法解析圖片內容，可能是檔案損壞或格式不支援");
                        }
                        using (var image = SKImage.FromBitmap(original))
                        using (var data = image.Encode(SKEncodedImageFormat.Webp, 90))
                        using (var blobUploadStream = data.AsStream())
                        {
                            var blobHttpHeader = new BlobHttpHeaders { ContentType = "image/webp" };
                            await blobClient.UploadAsync(blobUploadStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
                        }
                        //生成縮圖
                        float ratio = Math.Min(300f / original.Width, 300f / original.Height);
                        int width = (int)(original.Width * ratio);
                        int height = (int)(original.Height * ratio);

                        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                        using (var resized = original.Resize(new SKImageInfo(width, height), sampling))
                        using (var thumbImage = SKImage.FromBitmap(resized))
                        using (var thumbData = thumbImage.Encode(SKEncodedImageFormat.Webp, 80))
                        using (var thumbUploadStream = thumbData.AsStream())
                        {
                            var thumbHttpHeader = new BlobHttpHeaders { ContentType = "image/webp" };
                            await thumbBlobClient.UploadAsync(thumbUploadStream, new BlobUploadOptions { HttpHeaders = thumbHttpHeader });
                        }
                    }
                }
                //寫入資料庫
                var artwork = new Artwork
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId,
                    ImageUrl = blobClient.Uri.ToString(),
                    ThumbnailUrl = thumbBlobClient.Uri.ToString(),
                    CompletionDate = dto.CompletionDate,
                    CreatedAt = DateTime.Now
                };
                await _artworkRepository.AddAsync(artwork);
                await _artworkRepository.SaveChangesAsync();

                return ServiceResult<ArtworkResponse>.Ok(new ArtworkResponse
                {
                    ArtworkId = artwork.ArtworkId,
                    Title = artwork.Title,
                    FileUrl = artwork.ImageUrl,
                    ThumbnailUrl = artwork.ThumbnailUrl
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存作品至 Azure Blob 失敗，嘗試清理已上傳的雲端檔案: {Message}", ex.Message);
                await SafeDeleteBlobAsync(fileName);
                await SafeDeleteBlobAsync(thumbName);
                return ServiceResult<ArtworkResponse>.Fail($"儲存作品失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
        public async Task<ServiceResult<CursorPagedResult<ArtworkDto>>> GetPagedArtworksAsync(int limit = 10, string? cursor = null)
        {
            try
            {
                DateTime? cursorDate = null;
                int? cursorId = null;

                // 在 Service 層解析字串 
                if (!string.IsNullOrEmpty(cursor) && cursor.Contains('_'))
                {
                    var parts = cursor.Split('_');
                    if (parts.Length == 2 &&
                        DateTime.TryParse(parts[0], out var date) &&
                        int.TryParse(parts[1], out int id))
                    {
                        cursorDate = date;
                        cursorId = id;
                    }
                }

                //  呼叫 Repository
                var (data, hasNextPage) = await _artworkRepository.GetPagedArtworksAsync(limit, cursorDate, cursorId);

                // 3. 在 Service 層組裝下一個游標字串
                string? nextCursor = null;
                if (hasNextPage && data.Any())
                {
                    var lastItem = data.Last(); // 因為 Repo 已經 RemoveAt 了，這裏的 Last 就是最後一筆
                    nextCursor = $"{lastItem.CompletionDate:yyyy-MM-ddTHH:mm:ss.fff}_{lastItem.ArtworkId}";
                }

                var pagedResult = new CursorPagedResult<ArtworkDto>
                {
                    Data = data,
                    NextCursor = nextCursor
                };

                return ServiceResult<CursorPagedResult<ArtworkDto>>.Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得作品列表失敗: {Message}", ex.Message);
                return ServiceResult<CursorPagedResult<ArtworkDto>>.Fail($"取得作品列表失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
        public async Task<ServiceResult<ArtworkDto>> GetArtworkByIdAsync(int id)
        {
            try
            {
                var artwork = await _artworkRepository.GetDtoByIdAsync(id);

                if (artwork == null) return ServiceResult<ArtworkDto>.Fail("作品不存在", HttpStatusCode.NotFound);
                return ServiceResult<ArtworkDto>.Ok(artwork);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得作品 {ArtworkId} 失敗: {Message}", id, ex.Message);
                return ServiceResult<ArtworkDto>.Fail($"取得作品失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> DeleteArtworkAsync(int id)
        {
            var artwork = await _artworkRepository.GetByIdAsync(id);
            if (artwork == null) return ServiceResult.Fail("作品不存在", HttpStatusCode.NotFound);

            // 使用交易確保資料庫與雲端檔案同步刪除
            try
            {
                string? fileName = GetBlobNameFromUrl(artwork.ImageUrl);
                string? thumbName = GetBlobNameFromUrl(artwork.ThumbnailUrl);
                await _artworkRepository.ExecuteInTransactionAsync(async () =>
            {
                _artworkRepository.Remove(artwork);
                await _artworkRepository.SaveChangesAsync();
            });
                await SafeDeleteBlobAsync(fileName);
                await SafeDeleteBlobAsync(thumbName);
                return ServiceResult.Ok("作品刪除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除作品 {ArtworkId} 失敗: {Message}", id, ex.Message);
                return ServiceResult.Fail($"刪除作品失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }

        }
        public async Task<ServiceResult> UpdateArtworkAsync(ArtworkUpdateRequest dto)
        {
            var artwork = await _artworkRepository.GetByIdAsync(dto.ArtworkId);
            if (artwork == null) return ServiceResult.Fail("作品不存在", HttpStatusCode.NotFound);

            // 更新可修改的欄位
            if (!string.IsNullOrEmpty(dto.Title)) artwork.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.Description)) artwork.Description = dto.Description;
            if (dto.CategoryId.HasValue) artwork.CategoryId = dto.CategoryId.Value;
            if (dto.CompletionDate.HasValue) artwork.CompletionDate = dto.CompletionDate.Value;

            try
            {
                await _artworkRepository.SaveChangesAsync();
                return ServiceResult.Ok("作品更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新作品 {ArtworkId} 失敗: {Message}", dto.ArtworkId, ex.Message);
                return ServiceResult.Fail($"更新作品失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task SafeDeleteBlobAsync(string? blobName)
        {
            if (string.IsNullOrEmpty(blobName)) return;

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                if (response.Value)
                {
                    _logger.LogInformation("已成功清理 Azure Blob 檔案: {BlobName}", blobName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理 Azure Blob 檔案失敗: {BlobName}", blobName);
            }
        }
        private string? GetBlobNameFromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                var uri = new Uri(url);
                // uri.Segments.Last() 會取得 URL 最後一個部分（即檔名）
                return Uri.UnescapeDataString(uri.Segments.Last());
            }
            catch
            {
                return null;
            }
        }

    }
}