using Microsoft.EntityFrameworkCore;
using MyPortfolio.DTOs;
using MyPortfolio.Model;
using MyPortfolio.Model.Entities;
using MyPortfolio.Service.Interface;
using SkiaSharp;
using MyPortfolio.Repository;
using System.Net;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.Rendering;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MyPortfolio.Service
{
    public class JournalService : IJournalService
    {
        private readonly IJournalRepository _journalRepository;
        private readonly ILogger<JournalService> _logger;
        private readonly HtmlSanitizer _sanitizer;
        private readonly BlobContainerClient _containerClient;
        public JournalService(IJournalRepository repository, ILogger<JournalService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _journalRepository = repository;
            _sanitizer = new HtmlSanitizer();
            string? connectionString = configuration.GetSection("BlobStorage")["ConnetionString"];
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

        public async Task<ServiceResult<JournalResponseDto>> GetActiveDraftAsync()
        {
            var draft = await _journalRepository.GetActiveDraftAsync();
            if (draft == null) return ServiceResult<JournalResponseDto>.Fail("沒有草稿", HttpStatusCode.NotFound);
            return ServiceResult<JournalResponseDto>.Ok(MapToDto(draft));
        }
        public async Task<ServiceResult<JournalResponseDto>> SaveDraftAsync(JournalSaveRequest dto)
        {
            JournalEntry? entry = null;
            if (dto.Id.HasValue && dto.Id != Guid.Empty)
            {
                entry = await _journalRepository.GetByIdAsync(dto.Id.Value);

            }

            if (entry != null)
            {
                await UpdateEntryProperties(entry, dto);
            }
            else
            {
                entry = new JournalEntry
                {
                    Id = (dto.Id.HasValue && dto.Id != Guid.Empty) ? dto.Id.Value : Guid.NewGuid()
                };
                await UpdateEntryProperties(entry, dto);
                await _journalRepository.AddAsync(entry);
            }

            await _journalRepository.SaveChangesAsync();
            return ServiceResult<JournalResponseDto>.Ok(MapToDto(entry));
        }

        public async Task<ServiceResult<JournalResponseDto>> PublishAsync(JournalSaveRequest dto)
        {
            JournalEntry? entry = null;
            if (dto.Id.HasValue && dto.Id != Guid.Empty)
            {
                entry = await _journalRepository.GetByIdAsync(dto.Id.Value);
            }

            if (entry == null)
            {
                entry = new JournalEntry
                {
                    Id = (dto.Id.HasValue && dto.Id != Guid.Empty) ? dto.Id.Value : Guid.NewGuid(),
                    Status = JournalStatus.Published
                };
                await UpdateEntryProperties(entry, dto);
                await _journalRepository.AddAsync(entry);
            }
            else
            {

                await UpdateEntryProperties(entry, dto);
                entry.Status = JournalStatus.Published;
            }
            await _journalRepository.SaveChangesAsync();
            return ServiceResult<JournalResponseDto>.Ok(MapToDto(entry));
        }
        public async Task<ServiceResult<JournalImageUploadResponse>> UploadImageAsync(IFormFile file, Guid journalId)
        {
            var journalExists = await _journalRepository.GetByIdAsync(journalId);
            if (journalExists == null) return ServiceResult<JournalImageUploadResponse>.Fail("指定的日誌主體不存在，無法上傳圖片", HttpStatusCode.NotFound);

            var fileGuid = Guid.NewGuid();
            var fileName = $"journal/{fileGuid}.webp";
            var blobClient = _containerClient.GetBlobClient(fileName);
            try
            {
                using (var stream = file.OpenReadStream())
                using (var original = SKBitmap.Decode(stream))
                {
                    if (original == null) return ServiceResult<JournalImageUploadResponse>.Fail("無效圖片格式", HttpStatusCode.BadRequest);
                    using (var image = SKImage.FromBitmap(original))
                    using (var data = image.Encode(SKEncodedImageFormat.Webp, 85))
                    using (var blobUploadStream = data.AsStream())
                    {
                        var blobHttpHeader = new BlobHttpHeaders { ContentType = "image/webp" };
                        await blobClient.UploadAsync(blobUploadStream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
                    }
                }

                var imageUrl = blobClient.Uri.ToString();
                var imageGuid = Guid.NewGuid();
                var journalImage = new JournalImage
                {
                    Id = imageGuid,
                    JournalEntryId = journalId,
                    ImageUrl = imageUrl,
                    BlobName = fileName,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _journalRepository.AddImageAsync(journalImage);
                await _journalRepository.SaveChangesAsync();
                return ServiceResult<JournalImageUploadResponse>.Ok(new JournalImageUploadResponse { Id = imageGuid, ImageUrl = imageUrl });
            }
            catch (Exception ex)
            {

                await SafeDeleteBlobAsync(fileName);
                _logger.LogError(ex, "上傳日誌圖片失敗: {Message}", ex.Message);
                return ServiceResult<JournalImageUploadResponse>.Fail($"上傳圖片失敗: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<IEnumerable<JournalResponseDto>>> GetPublishedListAsync()
        {
            var list = await _journalRepository.GetPublishedListAsync();
            var dtoList = list.Select(MapToDto);
            return ServiceResult<IEnumerable<JournalResponseDto>>.Ok(dtoList);
        }
        public async Task<ServiceResult> DeleteJournalAsync(Guid id)
        {
            var entry = await _journalRepository.GetByIdAsync(id, includeImages: true);
            if (entry == null) return ServiceResult.Fail("找不到指定的日誌", HttpStatusCode.NotFound);

            if (entry.Images != null && entry.Images.Any())
            {
                foreach (var img in entry.Images)
                {
                    await SafeDeleteBlobAsync(img.BlobName);
                }
            }

            // 2. 刪除資料庫紀錄
            _journalRepository.Delete(entry);
            await _journalRepository.SaveChangesAsync();

            return ServiceResult.Ok("日誌與關聯圖片已成功刪除");
        }
        public async Task<ServiceResult> DeleteImageByIdAsync(Guid id)
        {
            var image = await _journalRepository.GetImageByIdAsync(id);
            if (image == null)
                return ServiceResult.Fail("找不到指定的圖片");

            // 2. 刪除伺服器硬碟上的實體檔案
            await SafeDeleteBlobAsync(image.BlobName);

            // 3. 刪除資料庫紀錄
            _journalRepository.DeleteImage(image);
            await _journalRepository.SaveChangesAsync();

            return ServiceResult.Ok("圖片刪除成功");
        }

        private async Task UpdateEntryProperties(JournalEntry entry, JournalSaveRequest dto)
        {
            entry.Title = dto.Title;
            entry.ContentJson = dto.ContentJson;
            entry.ContentHtml = _sanitizer.Sanitize(dto.ContentHtml);
            entry.JournalTags.Clear();
            foreach (var tagName in dto.Tags)
            {
                var tag = await _journalRepository.GetOrCreateTagAsync(tagName);
                entry.JournalTags.Add(tag);
            }
            entry.UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static JournalResponseDto MapToDto(JournalEntry entity) => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            ContentJson = entity.ContentJson,
            ContentHtml = entity.ContentHtml,
            Tags = entity.JournalTags.Select(t => t.Name).ToList(),
            Status = (int)entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ImageUrls = entity.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
        };
        // 輔助方法：安全刪除實體檔案，防止路徑穿越
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
    }

}