using MyPortfolio.Service.Interface;
using MyPortfolio.Model.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MyPortfolio.Common;
using System.Security.Claims;
using MyPortfolio.Repository;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace MyPortfolio.Service
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private const int AccessTokenMinutes = 15;
        private const int RefreshTokenDays = 14;
        private readonly IRefreshTokenRepository _refreshTokens; // 建構子注入

        // 256-bit 亂數，Base64Url 編碼（cookie 安全字元）
        private static string GeneratePlainToken() =>
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        private static string Hash(string plainToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger, IRefreshTokenRepository refreshTokens)
        {
            _configuration = configuration;
            _logger = logger;
            _refreshTokens = refreshTokens;
        }

        public string GenerateAccessToken(string username)
        {
            var jwtKey = _configuration.GetSection("Jwt")["Secret"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                _logger.LogError("JWT Key 未正確載入");
                throw new InvalidOperationException("JWT Key 未正確載入");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // 攜帶的使用者資訊
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
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<(string PlainToken, RefreshToken Record)> IssueRefreshTokenAsync(string email)
        {
            var plain = GeneratePlainToken();
            var record = new RefreshToken
            {
                TokenHash = Hash(plain),
                Email = email,
                FamilyId = Guid.NewGuid(), // 新登入 = 新家族
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
            };
            await _refreshTokens.DeleteExpiredAsync(); // 借登入的時機清過期資料
            await _refreshTokens.AddAsync(record);
            await _refreshTokens.SaveChangesAsync();
            return (plain, record);
        }

        public async Task<RefreshResult> RotateRefreshTokenAsync(string presentedPlainToken)
        {
            var record = await _refreshTokens.GetByHashAsync(Hash(presentedPlainToken));
            if (record == null)
                return RefreshResult.Fail();

            // 重用偵測：已被輪轉作廢的 token 又被拿出來用 → 可能被偷，整個家族作廢
            if (record.RevokedAtUtc != null)
            {
                _logger.LogWarning("偵測到 Refresh Token 重用，撤銷整個家族。Email: {Email}, Family: {FamilyId}",
                    record.Email, record.FamilyId);
                await _refreshTokens.RevokeFamilyAsync(record.FamilyId, DateTime.UtcNow);
                return RefreshResult.Fail();
            }

            if (record.ExpiresAtUtc <= DateTime.UtcNow)
                return RefreshResult.Fail();

            // 輪轉：舊的作廢，新的繼承家族與絕對過期時間（不延長壽命）
            record.RevokedAtUtc = DateTime.UtcNow;
            var newPlain = GeneratePlainToken();
            await _refreshTokens.AddAsync(new RefreshToken
            {
                TokenHash = Hash(newPlain),
                Email = record.Email,
                FamilyId = record.FamilyId,
                ExpiresAtUtc = record.ExpiresAtUtc
            });
            await _refreshTokens.SaveChangesAsync();

            return new RefreshResult
            {
                Success = true,
                NewAccessToken = GenerateAccessToken(record.Email),
                Email = record.Email,
                NewRefreshPlainToken = newPlain,
                RefreshExpiresAtUtc = record.ExpiresAtUtc
            };
        }

        public async Task RevokeByPlainTokenAsync(string presentedPlainToken)
        {
            var record = await _refreshTokens.GetByHashAsync(Hash(presentedPlainToken));
            if (record != null)
                await _refreshTokens.RevokeFamilyAsync(record.FamilyId, DateTime.UtcNow);
        }

    }
}