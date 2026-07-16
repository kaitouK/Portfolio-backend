using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MyPortfolio.DTOs
{
    //權限查詢時DTO
    public class AuthStatusDto
    {
        public bool IsAuthenticated { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? DisplayName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AccessToken { get; set; }
    }
}