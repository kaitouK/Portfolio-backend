using Microsoft.EntityFrameworkCore;
using MyPortfolio.Data;
using MyPortfolio.Model.Entities;
using MyPortfolio.DTOs;
namespace MyPortfolio.Repository
{
    public interface ICategoryRepository
    {
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<bool> ExistsAsync(int categoryId);
    }
    public class CategoryRepository : ICategoryRepository
    {
        private readonly MyDbContext _context;

        public CategoryRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                    .AsNoTracking()
                    .Select(c => new CategoryDto
                    {
                        CategoryId = c.CategoryId,
                        Name = c.Name
                    }).ToListAsync();

        }
        public async Task<bool> ExistsAsync(int categoryId)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

    }
}
