using MyPortfolio.Controller;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MyPortfolio.Service.Interface;
using MyPortfolio.DTOs;
using MyPortfolio.Service;
using System.Diagnostics;
using MyPortfolio.Model;
using System.Reflection.Metadata;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;

namespace MyPortfolio.Controller
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(IConfiguration configuration, IAuthService authService, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            try
            {
                // 從設定檔取得 Google Client ID
                var googleClientId = _configuration.GetSection("Authentication")["Google:ClientId"];
                if (string.IsNullOrEmpty(googleClientId))
                {
                    _logger.LogError("Google Client ID 未在設定檔中配置！");
                    return ProcessApiResponse(ApiResponse.Fail("伺服器內部錯誤", 500));
                }

                //驗證Audience，防止Token替換攻擊
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { googleClientId }
                };
                // 驗證前端傳來的 Google Token 是否合法 (包含確認是否由 Google 簽發、是否過期)
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
                if (!payload.EmailVerified)
                {
                    _logger.LogWarning("非法登入嘗試：{Email} 嘗試使用 Google 登入，但信箱未驗證。", payload.Email);
                    return ProcessApiResponse(ApiResponse.Fail("信箱未驗證，拒絕存取", 401));
                }

                // 白名單檢查
                var adminEmail = _configuration.GetSection("Admin")["Email"];
                if (payload.Email == null || !payload.Email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("非法登入嘗試：{Email} 嘗試使用 Google 登入，但不在白名單中。", payload.Email);
                    // 如果不是你本人的 Email，直接狠心拒絕，不給予任何有效的連線憑證
                    return ProcessApiResponse(ApiResponse.Fail("權限不足，拒絕存取。", 401));
                }

                // 通過白名單！呼叫 Service 產生專屬的 JWT
                var jwtToken = _authService.GenerateToken(payload.Email);

                // 將 JWT 寫入瀏覽器的 HttpOnly Cookie 中 30分過期
                Response.Cookies.Append("AppAuth", jwtToken, GetCookieOptions(DateTime.UtcNow.AddMinutes(30)));

                var authStatus = new AuthStatusDto
                {
                    IsAuthenticated = true,
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    Role = "admin"
                };

                return ProcessApiResponse(ApiResponse<object>.Ok(authStatus, "登入成功"));
            }
            catch (InvalidJwtException)
            {
                return ProcessApiResponse(ApiResponse.Fail("無效的 Google 憑證。", 400));
            }
        }
        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var authDto = new AuthStatusDto { IsAuthenticated = false };

            if (User.Identity?.IsAuthenticated == true)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                authDto.IsAuthenticated = true;
                authDto.Email = email;
                authDto.DisplayName = User.FindFirstValue(ClaimTypes.Name);
                authDto.Role = User.IsInRole("Admin") ? "admin" : "user"; // 直接從 JWT 內建的 Role 判斷


                RenewSession(email);
            }
            return ProcessApiResponse(ApiResponse<object>.Ok(authDto));

        }
        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("AppAuth", GetCookieOptions());
            return ProcessApiResponse(ApiResponse.Ok("已登出"));
        }
        private CookieOptions GetCookieOptions(DateTime? expires = null)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // 若為跨域開發環境，需保持 None；若同網域建議改為 Lax
                Path = "/",
                IsEssential = true
            };
            if (expires.HasValue)
            {
                options.Expires = expires.Value;
            }
            return options;
        }
        //自動續期
        private void RenewSession(string email)
        {
            var jwtToken = _authService.GenerateToken(email);
            Response.Cookies.Append("AppAuth", jwtToken, GetCookieOptions(DateTime.UtcNow.AddMinutes(30)));
        }

    }

    public class GoogleLoginDto { public string IdToken { get; set; } = string.Empty; }
}