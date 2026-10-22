using Microsoft.EntityFrameworkCore;
using MyPortfolio.DTOs;
using MyPortfolio.Common;
using MyPortfolio.Data;
using MyPortfolio.Service.Interface;
using MyPortfolio.Repository;

namespace MyPortfolio.Service
{
    /// <summary>
    /// GetAllCategoriesAsync：AsNoTracking 查詢投影成 CategoryDto。維持分層一致性。<br/>
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly MyDbContext _context;
        private readonly ILogger<CategoryService> _logger;
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(MyDbContext context, ILogger<CategoryService> logger, ICategoryRepository categoryRepository)
        {
            _context = context;
            _logger = logger;
            _categoryRepository = categoryRepository;
        }

        public async Task<ServiceResult<List<CategoryDto>>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAllCategoriesAsync();
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