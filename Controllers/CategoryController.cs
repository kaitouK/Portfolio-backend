using Microsoft.AspNetCore.Mvc;
using MyPortfolio.DTOs;
using MyPortfolio.Controller;
using MyPortfolio.Model;
using MyPortfolio.Service.Interface;
using Microsoft.EntityFrameworkCore;
[ApiController]
[Route("api/[controller]")]
public class CategoryController : BaseApiController
{

    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllCategoriesAsync();
        return ProcessApiResponse(result);
    }
}
