using Microsoft.EntityFrameworkCore;
using MyPortfolio.Model;
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
            return await _context.Set<JournalEntry>()
                .FirstOrDefaultAsync(j => j.Status == JournalStatus.Draft);
        }

        public async Task<JournalEntry?> GetByIdAsync(Guid id, bool includeImages = false)
        {
            var query = _context.Set<JournalEntry>().AsQueryable();

            if (includeImages)
            {
                query = query.Include(j => j.Images);
            }

            return await query.FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<List<JournalEntry>> GetPublishedListAsync()
        {
            var list = await _context.Set<JournalEntry>()
                .AsNoTracking()
                .Where(j => j.Status == JournalStatus.Published)
                .ToListAsync();
            return list.OrderByDescending(x => x.CreatedAt).ToList();
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
            var existingTag = await _context.JournalTags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == trimmedName.ToLower());

            if (existingTag != null)
            {
                return existingTag;
            }

            var newTag = new JournalTag { Id = Guid.NewGuid(), Name = trimmedName };
            await _context.JournalTags.AddAsync(newTag);
            return newTag;
        }
    }
}