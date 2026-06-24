using MyPortfolio.DTOs;
using MyPortfolio.Model;
namespace MyPortfolio.Service.Interface
{
    public interface IArtworkService
    {
        Task<ServiceResult<ArtworkResponse>> SaveArtworkAsync(ArtworkUploadRequest dto);
        Task<ServiceResult<IEnumerable<ArtworkDto>>> GetAllArtworksAsync();
        Task<ServiceResult<ArtworkDto>> GetArtworkByIdAsync(int id);
        Task<ServiceResult> DeleteArtworkAsync(int id);
        Task<ServiceResult> UpdateArtworkAsync(ArtworkUpdateRequest dto);
    }
}