using MyPortfolio.Model.Entities;
using MyPortfolio.Service;
using MyPortfolio.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace MyPortfolio.Tests.Service
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly SqliteInMemoryDb _db;
        private readonly CategoryService _service;
        public CategoryServiceTests()
        {
            _db = new SqliteInMemoryDb();
            _service = new CategoryService(_db.Context, NullLogger<CategoryService>.Instance);
        }
        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task GetAllCategoriesAsync_ReturnsSeededCategories()
        {
            var result = await _service.GetAllCategoriesAsync();
            var expected = await _db.Context.Categories.CountAsync();

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expected, result.Data.Count);
            Assert.Contains(result.Data, c => c.Name == "未分類");
            Assert.Contains(result.Data, c => c.Name == "線稿");
            Assert.Contains(result.Data, c => c.Name == "草圖");
            Assert.Contains(result.Data, c => c.Name == "成圖");
        }
        [Fact]
        public async Task GetAllCategoriesAsync_MapsIdAndNameCorrectly()
        {
            var result = await _service.GetAllCategoriesAsync();
            var uncategorized = result.Data!.Single(c => c.CategoryId == 1);
            Assert.Equal("未分類", uncategorized.Name);
        }
    }
}