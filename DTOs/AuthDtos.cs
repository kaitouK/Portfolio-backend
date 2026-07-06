using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MyPortfolio.DTOs
{
    //登入時獲取DTO
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
    //登入時回傳DTO
    public class LoginResponse
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; } = DateTime.Now.AddDays(1); // 預設 token 有效期為 1 天
    }
    //權限查詢時DTO
    public class AuthStatusDto
    {
        public bool IsAuthenticated { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? DisplayName { get; set; }
    }
}