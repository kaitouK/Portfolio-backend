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

namespace MyPortfolio.Service
{
    public class ArtworkService : IArtworkService
    {
        private readonly IArtworkRepository _artworkRepository;
        // 定義一個儲存上傳檔案的路徑，這裡使用 wwwroot/uploads 資料夾
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        private readonly ILogger<ArtworkService> _logger;
        public ArtworkService(IArtworkRepository artworkRepository, ILogger<ArtworkService> logger)
        {
            _artworkRepository = artworkRepository;
            _logger = logger;
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        }

        public async Task<ServiceResult<ArtworkResponse>> SaveArtworkAsync(ArtworkUploadRequest dto)
        {
            // 生成唯一檔名，避免重複 
            var fileGuid = Guid.NewGuid();
            var fileName = $"{fileGuid}.webp";// 原圖使用WebP格式
            var thumbName = $"{fileGuid}_thumb.webp"; // 縮圖使用 WebP 格式
            var filePath = Path.Combine(_storagePath, fileName);
            var thumbPath = Path.Combine(_storagePath, thumbName);
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
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            data.SaveTo(fileStream);
                        }
                        //生成縮圖
                        float ratio = Math.Min(300f / original.Width, 300f / original.Height);
                        int width = (int)(original.Width * ratio);
                        int height = (int)(original.Height * ratio);

                        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                        using (var resized = original.Resize(new SKImageInfo(width, height), sampling))
                        using (var thumbImage = SKImage.FromBitmap(resized))
                        using (var thumbData = thumbImage.Encode(SKEncodedImageFormat.Webp, 80))
                        using (var thumbStream = new FileStream(thumbPath, FileMode.Create))
                        {
                            thumbData.SaveTo(thumbStream);
                        }
                    }
                }
                //寫入資料庫
                var artwork = new Artwork
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId,
                    ImageUrl = $"/uploads/{fileName}",
                    ThumbnailUrl = $"/uploads/{thumbName}",
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
                _logger.LogError(ex, "儲存作品失敗，嘗試清理檔案: {Message}", ex.Message);
                CleanupFiles(filePath, thumbPath);
                return ServiceResult<ArtworkResponse>.Fail($"儲存作品失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
        public async Task<ServiceResult<IEnumerable<ArtworkDto>>> GetAllArtworksAsync()
        {
            try
            {
                var artworks = await _artworkRepository.GetAllWithDtoAsync();
                return ServiceResult<IEnumerable<ArtworkDto>>.Ok(artworks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得作品列表失敗: {Message}", ex.Message);
                return ServiceResult<IEnumerable<ArtworkDto>>.Fail($"取得作品列表失敗: {ex.Message}", HttpStatusCode.InternalServerError);
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

            // 刪除實體檔案
            string? filePath = null;
            if (!string.IsNullOrEmpty(artwork.ImageUrl))
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", artwork.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }
            string? thumbPath = null;
            if (!string.IsNullOrEmpty(artwork.ThumbnailUrl))
            {
                thumbPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", artwork.ThumbnailUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }

            // 刪除資料庫紀錄

            try
            {
                await _artworkRepository.ExecuteInTransactionAsync(async () =>
            {
                _artworkRepository.Remove(artwork);
                await _artworkRepository.SaveChangesAsync();
            });
                CleanupFiles(filePath, thumbPath);
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

        private void CleanupFiles(params string?[] paths)
        {
            foreach (var path in paths.Where(p => !string.IsNullOrEmpty(p)))
            {
                try
                {
                    var fullPath = Path.GetFullPath(path!);
                    if (fullPath.StartsWith(_storagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            _logger.LogInformation("已清理檔案: {FilePath}", fullPath);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("拒絕刪除目錄外的非法路徑: {FilePath}", fullPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理檔案失敗: {FilePath}", path);
                }
            }
        }

    }
}