using MyPortfolio.Service.Interface;
using MyPortfolio.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MyPortfolio.Model;
using System.Security.Claims;
namespace MyPortfolio.Service
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(string username)
        {
            var jwtKey = _configuration.GetSection("Jwt")["Secret"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                _logger.LogError("JWT Key 未正確載入");
                throw new InvalidOperationException("JWT Key 未正確載入");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Admin"),
                new Claim(ClaimTypes.Email, username),
                new Claim(ClaimTypes.Role, "Admin") // 這裡賦予 Administrator 角色
            };
            //JWT簽發
            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("Jwt")["Issuer"],
                audience: _configuration.GetSection("Jwt")["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}