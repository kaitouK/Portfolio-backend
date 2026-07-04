using MyPortfolio.DTOs;
using MyPortfolio.Model;
namespace MyPortfolio.Service.Interface
{
    public interface IJournalService
    {
        Task<ServiceResult<JournalResponseDto>> GetActiveDraftAsync();
        Task<ServiceResult<JournalResponseDto>> SaveDraftAsync(JournalSaveRequest dto);
        Task<ServiceResult<JournalResponseDto>> PublishAsync(JournalSaveRequest dto);
        Task<ServiceResult<JournalImageUploadResponse>> UploadImageAsync(IFormFile file, Guid journalId);
        Task<ServiceResult<IEnumerable<JournalResponseDto>>> GetPublishedListAsync();
        Task<ServiceResult> DeleteJournalAsync(Guid id);
        Task<ServiceResult> DeleteImageByIdAsync(Guid id);
    }
}