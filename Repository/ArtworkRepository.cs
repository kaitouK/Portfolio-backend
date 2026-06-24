using Microsoft.EntityFrameworkCore;
using MyPortfolio.Model;
using MyPortfolio.Model.Entities;
using MyPortfolio.DTOs;
namespace MyPortfolio.Repository
{
    public static class ArtworkExtensions
    {
        public static IQueryable<ArtworkDto> ProjectToDto(this IQueryable<Artwork> query)
        {
            return query.Select(a => new ArtworkDto
            {
                ArtworkId = a.ArtworkId,
                Title = a.Title,
                Description = a.Description,
                ImageUrl = a.ImageUrl,
                // 這裡的邏輯會被 EF 轉譯成 SQL 的 CASE WHEN 或 JOIN
                CategoryId = a.CategoryId,
                Tags = a.Tags.Select(t => t.TagName).ToList(),
                PixivViews = a.ExternalStat != null ? (int?)a.ExternalStat.PixivViews : null,
                CompletionDate = a.CompletionDate,
                CreatedAt = a.CreatedAt
            });
        }
    }
    public interface IArtworkRepository
    {
        // 基本 CRUD
        Task<Artwork?> GetByIdAsync(int id);
        Task AddAsync(Artwork artwork);
        void Remove(Artwork artwork);
        Task SaveChangesAsync();

        // 複雜查詢
        Task<IEnumerable<ArtworkDto>> GetAllWithDtoAsync();
        Task<ArtworkDto?> GetDtoByIdAsync(int id);

        // 事務處理
        Task ExecuteInTransactionAsync(Func<Task> action);
    }
    public class ArtworkRepository : IArtworkRepository
    {
        private readonly MyDbContext _context;

        public ArtworkRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<Artwork?> GetByIdAsync(int id) => await _context.Artworks.FindAsync(id);

        public async Task AddAsync(Artwork artwork) => await _context.Artworks.AddAsync(artwork);

        public void Remove(Artwork artwork) => _context.Artworks.Remove(artwork);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await action();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw; // 重新拋出讓 Service 處理錯誤
            }
        }

        // 將複雜的 Select 邏輯封裝在這裡
        public async Task<IEnumerable<ArtworkDto>> GetAllWithDtoAsync()
        {
            return await _context.Artworks
                .Where(a => a.IsGalleryVisible)
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ProjectToDto()
                .ToListAsync();
        }

        public async Task<ArtworkDto?> GetDtoByIdAsync(int id)
        {
            return await _context.Artworks
                .AsNoTracking()
                .Where(a => a.ArtworkId == id)
                .ProjectToDto()
                .FirstOrDefaultAsync();
        }

    }
}