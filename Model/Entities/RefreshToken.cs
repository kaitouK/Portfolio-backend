using System.ComponentModel.DataAnnotations;

namespace MyPortfolio.Model.Entities
{
    /// <summary>
    /// Refresh Token 紀錄。只存 SHA-256 雜湊，不存明文。
    /// FamilyId：同一次登入的所有 token 屬同一家族，輪轉時繼承；
    /// 偵測到已作廢的 token 被重用時，整個家族一起撤銷。
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(64)] // SHA-256 hex 固定 64 字元
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        public Guid FamilyId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // 絕對過期：輪轉時繼承、不延長。登入後最多活 14 天。
        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }
    }
}