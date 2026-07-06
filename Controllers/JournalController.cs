using MyPortfolio.DTOs;
using Microsoft.AspNetCore.Mvc;
using MyPortfolio.Common;
using MyPortfolio.Service.Interface;
using System.Net;
using MyPortfolio.Controller;
using System.Diagnostics;
using MyPortfolio.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class JournalController : BaseApiController
{
    private readonly IJournalService _journalService;
    private readonly IImageValidator _imageValidator;
    private readonly ILogger<JournalController> _logger;
    public JournalController(IJournalService journalService, ILogger<JournalController> logger, IImageValidator imageValidator)
    {
        _journalService = journalService;
        _logger = logger;
        _imageValidator = imageValidator;
    }
    [HttpGet("draft")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetActiveDraft()
    {
        var result = await _journalService.GetActiveDraftAsync();
        return ProcessApiResponse(result);
    }
    [HttpPost("draft")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SaveDraft([FromBody] JournalSaveRequest dto)
    {
        var result = await _journalService.SaveDraftAsync(dto);
        return ProcessApiResponse(result);
    }

    [HttpPost("publish")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Publish([FromBody] JournalSaveRequest dto)
    {
        var result = await _journalService.PublishAsync(dto);
        return ProcessApiResponse(result);
    }
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _journalService.DeleteJournalAsync(id);
        return ProcessApiResponse(result);
    }
    [HttpPost("image")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromQuery] Guid journalId)
    {
        if (file == null || file.Length == 0)
            return ProcessApiResponse(ApiResponse.Fail("請選擇檔案", 400));

        // 2. 限制檔案大小 (8MB)
        const long maxFileSize = 8 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return ProcessApiResponse(ApiResponse.Fail($"檔案大小不能超過 {maxFileSize / (1024 * 1024)} MB", 400));

        // 3. 限制檔案格式
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return ProcessApiResponse(ApiResponse.Fail("不支援的檔案格式", 400));

        // 4. 內容魔術數字驗證 (防止假改副檔名的偽裝檔)
        using (var stream = file.OpenReadStream())
        {
            if (!_imageValidator.IsValidImage(stream, out string detectedFormat))
            {
                _logger.LogWarning("文章插圖上傳 {FileName} 內容驗證失敗，檢測格式：{DetectedFormat}", file.FileName, detectedFormat);
                return ProcessApiResponse(ApiResponse.Fail("檔案內容不是有效的圖片", 400));
            }
        }
        try
        {
            var result = await _journalService.UploadImageAsync(file, journalId);
            return ProcessApiResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "日誌圖片上傳發生未預期錯誤");
            return ProcessApiResponse(ApiResponse.Fail("上傳失敗，請稍後再試", 500));
        }
    }
    [HttpDelete("image/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteImage([FromRoute] Guid id)
    {
        var result = await _journalService.DeleteImageByIdAsync(id);
        return ProcessApiResponse(result);
    }
    [HttpGet]
    public async Task<IActionResult> GetPublishedList()
    {
        var result = await _journalService.GetPublishedListAsync();
        return ProcessApiResponse(result);
    }
}