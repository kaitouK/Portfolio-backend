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
                ThumbnailUrl = a.ThumbnailUrl,
                DisplayUrl = a.DisplayUrl,
                ImageWidth = a.ImageWidth,
                ImageHeight = a.ImageHeight,
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
        Task<List<ArtworkIndexItemDto>> GetIndexAsync();
        // 從 cursor 之後到目標（含）有幾筆；目標不存在或隱藏時回 null
        Task<int?> CountThroughAsync(DateTime? cursorDate, int? cursorId, int untilId);
        // 還沒有寬高的作品（含隱藏），依 Id 遞增，回傳 tracked 實體供補資料修改
        Task<List<Artwork>> GetMissingImageSizeAsync(int afterId, int take);

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
            var artworks = await VisibleAfterCursor(cursorDate, cursorId)
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

        public async Task<List<ArtworkIndexItemDto>> GetIndexAsync()
        {
            // 排序必須和 GetPagedArtworksAsync 一致，左側索引與中間清單的順序才對得上
            return await _context.Artworks
                .AsNoTracking()
                .Where(a => a.IsGalleryVisible)
                .OrderByDescending(a => a.CompletionDate)
                .ThenByDescending(a => a.ArtworkId)
                .Select(a => new ArtworkIndexItemDto
                {
                    ArtworkId = a.ArtworkId,
                    Title = a.Title,
                    ThumbnailUrl = a.ThumbnailUrl
                })
                .ToListAsync();
        }

        public async Task<int?> CountThroughAsync(DateTime? cursorDate, int? cursorId, int untilId)
        {
            var target = await _context.Artworks
                .AsNoTracking()
                .Where(a => a.ArtworkId == untilId && a.IsGalleryVisible)
                .Select(a => new { a.CompletionDate, a.ArtworkId })
                .FirstOrDefaultAsync();
            if (target == null) return null;

            var targetDate = target.CompletionDate;
            var targetId = target.ArtworkId;
            // 排序是遞減，所以「排在目標之前（含目標）」= 日期較新，或同日期且 Id 較大
            return await VisibleAfterCursor(cursorDate, cursorId)
                .CountAsync(a => a.CompletionDate > targetDate ||
                                 (a.CompletionDate == targetDate && a.ArtworkId >= targetId));
        }

        public async Task<List<Artwork>> GetMissingImageSizeAsync(int afterId, int take)
        {
            return await _context.Artworks
                .Where(a => a.ImageWidth == null && a.ArtworkId > afterId)
                .OrderBy(a => a.ArtworkId)
                .Take(take)
                .ToListAsync();
        }

        // 公開作品 + 游標過濾（只處理純粹的資料過濾邏輯）
        private IQueryable<Artwork> VisibleAfterCursor(DateTime? cursorDate, int? cursorId)
        {
            var query = _context.Artworks
                .Where(a => a.IsGalleryVisible)
                .AsNoTracking();

            if (cursorDate.HasValue && cursorId.HasValue)
            {
                query = query.Where(a =>
                    a.CompletionDate < cursorDate.Value ||
                    (a.CompletionDate == cursorDate.Value && a.ArtworkId < cursorId.Value));
            }
            return query;
        }

    }
}