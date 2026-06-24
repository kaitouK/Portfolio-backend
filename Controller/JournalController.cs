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
public class JournalController : BaseApiController
{
    private readonly IJournalService _journalService;
    private readonly ILogger<JournalController> _logger;
    public JournalController(IJournalService journalService, ILogger<JournalController> logger)
    {
        _journalService = journalService;
        _logger = logger;
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
            return ProcessApiResponse(ApiResponse.Fail("請選擇檔案"));
        var result = await _journalService.UploadImageAsync(file, journalId);
        return ProcessApiResponse(result);
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