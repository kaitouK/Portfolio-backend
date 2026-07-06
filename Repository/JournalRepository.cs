using Microsoft.EntityFrameworkCore;
using MyPortfolio.Data;
using MyPortfolio.Model.Entities;
namespace MyPortfolio.Repository
{
    public interface IJournalRepository
    {
        Task<JournalEntry?> GetActiveDraftAsync();
        Task<JournalEntry?> GetByIdAsync(Guid id, bool includeImages = false);
        Task<List<JournalEntry>> GetPublishedListAsync();
        Task AddAsync(JournalEntry entry);
        Task AddImageAsync(JournalImage image);
        void Delete(JournalEntry entry);
        Task<int> SaveChangesAsync();
        Task<JournalImage?> GetImageByIdAsync(Guid id);
        void DeleteImage(JournalImage image);

        Task<JournalTag> GetOrCreateTagAsync(string tagName);
    }

    public class JournalRepository : IJournalRepository
    {
        private readonly MyDbContext _context;

        public JournalRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<JournalEntry?> GetActiveDraftAsync()
        {
            return await _context.JournalEntries
                .Include(j => j.JournalTags)
                .Include(j => j.Images)
                .FirstOrDefaultAsync(j => j.Status == JournalStatus.Draft);
        }

        public async Task<JournalEntry?> GetByIdAsync(Guid id, bool includeImages = false)
        {
            var query = _context.Set<JournalEntry>().AsQueryable();

            if (includeImages)
            {
                query = query.Include(j => j.Images);
            }

            return await query
                .Include(j => j.JournalTags)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<List<JournalEntry>> GetPublishedListAsync()
        {
            //避免not supported exception，先將資料撈出，再由CPU排序
            var list = await _context.JournalEntries
               .Include(j => j.JournalTags)
               .Include(j => j.Images)
               .AsNoTracking()
               .Where(j => j.Status == JournalStatus.Published)
               .ToListAsync();
            return list.OrderByDescending(j => j.CreatedAt).ToList();
        }

        public async Task AddAsync(JournalEntry entry)
        {
            await _context.Set<JournalEntry>().AddAsync(entry);
        }

        public async Task AddImageAsync(JournalImage image)
        {
            await _context.Set<JournalImage>().AddAsync(image);
        }

        public void Delete(JournalEntry entry)
        {
            _context.Set<JournalEntry>().Remove(entry);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public async Task<JournalImage?> GetImageByIdAsync(Guid id)
        {
            return await _context.Set<JournalImage>().FirstOrDefaultAsync(img => img.Id == id);
        }

        public void DeleteImage(JournalImage image)
        {
            _context.Set<JournalImage>().Remove(image);
        }
        public async Task<JournalTag> GetOrCreateTagAsync(string tagName)
        {
            var trimmedName = tagName.Trim();
            //1. 先從EF Core的本地追蹤快取中尋找(避免同一次Request內重複new導致衝突)
            var localTag = _context.JournalTags.Local
                .FirstOrDefault(t => t.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
            if (localTag != null)
            {
                return localTag;
            }
            //2. 再從資料庫中尋找
            var existingTag = await _context.JournalTags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == trimmedName.ToLower());

            if (existingTag != null)
            {
                return existingTag;
            }

            //3. 都不存在就建立新標籤
            var newTag = new JournalTag { Id = Guid.NewGuid(), Name = trimmedName };
            await _context.JournalTags.AddAsync(newTag);
            return newTag;
        }
    }
}