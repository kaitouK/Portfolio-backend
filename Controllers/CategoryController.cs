using Microsoft.AspNetCore.Mvc;
using MyPortfolio.DTOs;
using MyPortfolio.Controller;
using MyPortfolio.Model;
using Microsoft.EntityFrameworkCore;
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : BaseApiController
    {
        private readonly MyDbContext _context;
        public CategoryController(MyDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name
            }).ToListAsync();
            return ProcessApiResponse(ApiResponse<List<CategoryDto>>.Ok(categories));
        }
    }
