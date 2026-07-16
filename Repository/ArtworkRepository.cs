using Microsoft.EntityFrameworkCore;
using MyPortfolio.Data;
using MyPortfolio.Model.Entities;
using MyPortfolio.DTOs;
namespace MyPortfolio.Repository
{
    public static class ArtworkExtensions
    {
        public static IQueryable<ArtworkDto> ProjectToDto(this IQueryable<Artwork> query)
        {
            // 只SELECT需要映射的欄位，沒有寫入的不會被查詢
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
        Task<(IEnumerable<ArtworkDto> Data, bool HasNextPage)> GetPagedArtworksAsync(int limit,
    DateTime? cursorDate,
    int? cursorId);
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
        public async Task<(IEnumerable<ArtworkDto> Data, bool HasNextPage)> GetPagedArtworksAsync(
    int limit,
    DateTime? cursorDate,
    int? cursorId)
        {
            var query = _context.Artworks
                .Where(a => a.IsGalleryVisible)
                .AsNoTracking();

            // 只處理純粹的資料過濾邏輯
            if (cursorDate.HasValue && cursorId.HasValue)
            {
                query = query.Where(a =>
                    a.CompletionDate < cursorDate.Value ||
                    (a.CompletionDate == cursorDate.Value && a.ArtworkId < cursorId.Value));
            }

            var artworks = await query
                .OrderByDescending(a => a.CompletionDate)
                .ThenByDescending(a => a.ArtworkId)
                .Take(limit + 1) // 依然多拿一筆來判斷有無下一頁
                .ProjectToDto()
                .ToListAsync();

            bool hasNextPage = artworks.Count > limit;

            if (hasNextPage)
            {
                artworks.RemoveAt(limit); // 移除多抓的那一筆
            }

            return (artworks, hasNextPage);
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