using Microsoft.EntityFrameworkCore;
using MyPortfolio.DTOs;
using MyPortfolio.Model;
using MyPortfolio.Service.Interface;

namespace MyPortfolio.Service
{
    public class CategoryService : ICategoryService
    {
        private readonly MyDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(MyDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<List<CategoryDto>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .AsNoTracking()
                    .Select(c => new CategoryDto
                    {
                        CategoryId = c.CategoryId,
                        Name = c.Name
                    }).ToListAsync();

                return ServiceResult<List<CategoryDto>>.Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得分類列表失敗");
                return ServiceResult<List<CategoryDto>>.Fail("無法取得分類列表", System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}