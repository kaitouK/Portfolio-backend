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
using MyPortfolio.Common;
using System.Reflection.Metadata;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace MyPortfolio.Controller
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private const string AccessCookieName = "__Host-AppAuth";
        private const string RefreshCookieName = "__Secure-AppRefresh";
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
                var accessToken = _authService.GenerateAccessToken(payload.Email);

                var (refreshPlain, refreshRecord) = await _authService.IssueRefreshTokenAsync(payload.Email);

                var authStatus = new AuthStatusDto
                {
                    IsAuthenticated = true,
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    Role = "admin",
                    AccessToken = accessToken //改帶入accesstoken進 authstatusDto
                };
                _logger.LogInformation("Google 登入成功並已簽發 JWT。用戶資訊 -> Email: {Email}, Name: {Name},Location: {Location}", payload.Email, payload.Name, payload.Locale);
                SetAuthCookies(refreshPlain, refreshRecord.ExpiresAtUtc);
                return ProcessApiResponse(ApiResponse<object>.Ok(authStatus, "登入成功"));
            }
            catch (InvalidJwtException)
            {
                return ProcessApiResponse(ApiResponse.Fail("無效的 Google 憑證。", 400));
            }
        }
        private CookieOptions GetCookieOptions(DateTime? expires = null, string path = "/")
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // 若為跨域開發環境，需保持 None；若同網域建議改為 Lax
                Path = path,
                IsEssential = true
            };
            if (expires.HasValue)
            {
                options.Expires = expires.Value;
            }
            return options;
        }
        private void SetAuthCookies(string refreshPlain, DateTime refreshExpiresAtUtc)
        {
            Response.Cookies.Append(RefreshCookieName, refreshPlain,
                GetCookieOptions(refreshExpiresAtUtc, "/api/auth")); // 只在 auth 端點出現
        }

        private void DeleteAuthCookies()
        {
            Response.Cookies.Delete(RefreshCookieName, GetCookieOptions(path: "/api/auth"));
        }
        [AllowAnonymous] // access token 過期時也要能打，靠 refresh cookie 本身驗證
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var presented = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrEmpty(presented))
                return ProcessApiResponse(ApiResponse.Fail("未提供憑證", 401));

            var result = await _authService.RotateRefreshTokenAsync(presented);
            if (!result.Success)
            {
                DeleteAuthCookies(); // 無效/過期/重用 → 清乾淨，強制重新登入
                return ProcessApiResponse(ApiResponse.Fail("憑證已失效，請重新登入", 401));
            }
            var authStatus = new AuthStatusDto
            {
                IsAuthenticated = true,
                Email = result.Email,
                Role = "admin",
                AccessToken = result.NewAccessToken

            };

            SetAuthCookies(result.NewRefreshPlainToken!, result.RefreshExpiresAtUtc);
            return ProcessApiResponse(ApiResponse<AuthStatusDto>.Ok(authStatus, "已更新憑證"));
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var presented = Request.Cookies[RefreshCookieName];
            if (!string.IsNullOrEmpty(presented))
                await _authService.RevokeByPlainTokenAsync(presented); // 登出 = 撤銷整個家族

            DeleteAuthCookies();
            return ProcessApiResponse(ApiResponse.Ok("已登出"));
        }

    }

    public class GoogleLoginDto { public string IdToken { get; set; } = string.Empty; }
}