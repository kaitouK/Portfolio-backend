using MyPortfolio.DTOs;
using Microsoft.AspNetCore.Mvc;
using MyPortfolio.Model;
using MyPortfolio.Service.Interface;
using System.Net;
using MyPortfolio.Controller;
using System.Diagnostics;
using MyPortfolio.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
[ApiController]
[Route("api/[controller]")]
public class ArtworksController : BaseApiController
{
    private readonly IArtworkService _artworkService;
    private readonly IImageValidator _imageValidator;
    private readonly ILogger<ArtworksController> _logger;

    public ArtworksController(IArtworkService artworkService, IImageValidator imageValidator, ILogger<ArtworksController> logger)
    {
        _artworkService = artworkService;
        _imageValidator = imageValidator;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Upload([FromForm] ArtworkUploadRequest dto)
    {
        // 基本驗證
        if (dto.File == null || dto.File.Length == 0)
            return ProcessApiResponse(ApiResponse.Fail("請選擇要上傳的圖片", 400));

        // 1.限制檔案大小
        const long maxFileSize = 8 * 1024 * 1024; //8MB
        if (dto.File.Length > maxFileSize)
            return ProcessApiResponse(ApiResponse.Fail($"檔案大小不能超過 {maxFileSize / (1024 * 1024)} MB", 400));

        // 2. 限制檔案格式 (安全性考量)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return ProcessApiResponse(ApiResponse.Fail("不支援的檔案格式", 400));

        //3. 內容驗證 (ImageValidator檢查)
        using (var stream = dto.File.OpenReadStream())
        {
            if (!_imageValidator.IsValidImage(stream, out string detectedFormat))
            {
                _logger.LogWarning("上傳檔案 {FileName} 內容驗證失敗，檢測到格式為 {DetectedFormat}", dto.File.FileName, detectedFormat);
                return ProcessApiResponse(ApiResponse.Fail("檔案內容不是有效的圖片", 400));
            }
        }
        try
        {
            // 3. 呼叫 Service 處理儲存邏輯
            var result = await _artworkService.SaveArtworkAsync(dto);

            return ProcessApiResponse(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "上傳作品時發生參數錯誤: {Message}", ex.Message);
            return ProcessApiResponse(ApiResponse<ArtworkResponse>.Fail($"上傳失敗: {ex.Message}", 400));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "上傳作品時發生無效操作: {Message}", ex.Message);
            return ProcessApiResponse(ApiResponse<ArtworkResponse>.Fail("上傳失敗，請檢查輸入資料", 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上傳作品時發生未預期錯誤: {Message}", ex.Message);
            return ProcessApiResponse(ApiResponse<ArtworkResponse>.Fail("上傳失敗，請稍後再試", 500));
        }
    }
    //網址: GET /api/artworks
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _artworkService.GetAllArtworksAsync();
            return ProcessApiResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得所有作品失敗: {Message}", ex.Message);
            return ProcessApiResponse(ApiResponse.Fail("取得作品列表失敗，請稍後再試", 500));
        }
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _artworkService.GetArtworkByIdAsync(id);
            return ProcessApiResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得作品 {ArtworkId} 失敗: {Message}", id, ex.Message);
            return ProcessApiResponse(ApiResponse.Fail("取得作品失敗，請稍後再試", 500));
        }
    }
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _artworkService.DeleteArtworkAsync(id);
            return ProcessApiResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除作品 {ArtworkId} 失敗: {Message}", id, ex.Message);
            return ProcessApiResponse(ApiResponse.Fail($"刪除作品失敗: {ex.Message}", 500));
        }
    }
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] ArtworkUpdateRequest dto)
    {
        if (id != dto.ArtworkId)
        {
            return ProcessApiResponse(ApiResponse.Fail("URL 中的 ID 與請求體中的 ID 不一致", 400));
        }
        try
        {
            var result = await _artworkService.UpdateArtworkAsync(dto);
            return ProcessApiResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新作品 {ArtworkId} 失敗: {Message}", id, ex.Message);
            return ProcessApiResponse(ApiResponse.Fail($"更新作品失敗: {ex.Message}", 500));
        }
    }
}