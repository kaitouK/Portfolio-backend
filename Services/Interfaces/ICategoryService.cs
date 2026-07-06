using MyPortfolio.DTOs;
using MyPortfolio.Common;

namespace MyPortfolio.Service.Interface
{
    public interface ICategoryService
    {
        Task<ServiceResult<List<CategoryDto>>> GetAllCategoriesAsync();
    }
}