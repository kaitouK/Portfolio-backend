using MyPortfolio.DTOs;
using MyPortfolio.Model;
namespace MyPortfolio.Service.Interface
{
    public interface IArtworkService
    {
        Task<ServiceResult<ArtworkResponse>> SaveArtworkAsync(ArtworkUploadRequest dto);
        Task<ServiceResult<CursorPagedResult<ArtworkDto>>> GetPagedArtworksAsync(int limit, string? cursor);
        Task<ServiceResult<ArtworkDto>> GetArtworkByIdAsync(int id);
        Task<ServiceResult> DeleteArtworkAsync(int id);
        Task<ServiceResult> UpdateArtworkAsync(ArtworkUpdateRequest dto);
    }
}