using MyPortfolio.DTOs;
using MyPortfolio.Common;
namespace MyPortfolio.Service.Interface
{
    public interface IArtworkService
    {
        Task<ServiceResult<ArtworkResponse>> SaveArtworkAsync(ArtworkUploadRequest dto);
        Task<ServiceResult<CursorPagedResult<ArtworkDto>>> GetPagedArtworksAsync(int limit, string? cursor, int? untilId = null);
        Task<ServiceResult<List<ArtworkIndexItemDto>>> GetArtworkIndexAsync();
        Task<ServiceResult<ArtworkDto>> GetArtworkByIdAsync(int id);
        Task<ServiceResult> DeleteArtworkAsync(int id);
        Task<ServiceResult> UpdateArtworkAsync(ArtworkUpdateRequest dto);
        Task<ServiceResult<ImageBackfillResult>> BackfillImagesAsync(int afterId, int batchSize);
    }
}