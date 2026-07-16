using MyPortfolio.Common;
using MyPortfolio.Model.Entities;
namespace MyPortfolio.Service.Interface
{
    public interface IAuthService
    {
        string GenerateAccessToken(string email);
        Task<(string PlainToken, RefreshToken Record)> IssueRefreshTokenAsync(string email); // 登入：開新家族
        Task<RefreshResult> RotateRefreshTokenAsync(string presentedPlainToken);     // 輪轉
        Task RevokeByPlainTokenAsync(string presentedPlainToken);
    }
}