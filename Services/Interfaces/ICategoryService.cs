using MyPortfolio.DTOs;
using MyPortfolio.Model;

namespace MyPortfolio.Service.Interface
{
    public interface ICategoryService
    {
        Task<ServiceResult<List<CategoryDto>>> GetAllCategoriesAsync();
    }
}