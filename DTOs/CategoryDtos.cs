using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MyPortfolio.DTOs
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}