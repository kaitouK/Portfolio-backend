using Microsoft.EntityFrameworkCore;
using MyPortfolio.Data;
using MyPortfolio.Model.Entities;
namespace MyPortfolio.Repository
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash);
        Task AddAsync(RefreshToken token);
        Task RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc);
        Task DeleteExpiredAsync(); // 順手清過期資料，避免表無限長大
        Task SaveChangesAsync();
    }
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly MyDbContext _context;
        public RefreshTokenRepository(MyDbContext context) => _context = context;

        public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
            _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        public async Task AddAsync(RefreshToken token) =>
            await _context.RefreshTokens.AddAsync(token);

        public Task RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc) =>
            _context.RefreshTokens
               .Where(rt => rt.FamilyId == familyId && rt.RevokedAtUtc == null)
               .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAtUtc, revokedAtUtc));

        public Task DeleteExpiredAsync() =>
            _context.RefreshTokens
               .Where(rt => rt.ExpiresAtUtc < DateTime.UtcNow)
               .ExecuteDeleteAsync();

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

}