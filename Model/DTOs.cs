using System.ComponentModel.DataAnnotations;
using MyPortfolio.Model.Entities;
namespace MyPortfolio.DTOs
{
    public class ArtworkUploadRequest
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        [Required]
        // IFormFile 用於接收前端傳來的二進位檔案
        public required IFormFile File { get; set; }
        // 新增：作品完成日期，預設為上傳時間，前端可以選擇傳入具體日期
        public required DateTime CompletionDate { get; set; } = DateTime.Now;
    }
    // ARTWORK上傳時回傳DTO
    public class ArtworkResponse
    {
        public int ArtworkId { get; set; }
        public required string Title { get; set; }
        public required string FileUrl { get; set; } // 儲存檔案的 URL
        public required string ThumbnailUrl { get; set; } // 縮圖 URL
    }
    // ARTWORK查詢時回傳DTO
    public class ArtworkDto
    {
        public int ArtworkId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
        public int? PixivViews { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime CompletionDate { get; set; } // 作品完成日期

    }
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
    //更新時獲取DTO
    public class ArtworkUpdateRequest
    {
        public int ArtworkId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? CompletionDate { get; set; } // 作品完成日期
    }
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    //權限查詢時DTO
    public class AuthStatusDto
    {
        public bool IsAuthenticated { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? DisplayName { get; set; }
    }
    public class JournalSaveRequest
    {
        public Guid? Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "Untitled";
        [Required]
        public string ContentJson { get; set; } = "{}";
        [Required]
        public string ContentHtml { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
    public class JournalResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentJson { get; set; } = "{}";//內含<img>網址供tiptap還原
        public string ContentHtml { get; set; } = string.Empty;//內含<img>網址供前台渲染
        public List<string> Tags { get; set; } = new();
        public int Status { get; set; } = 0;//0:草稿 1:發布
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
    public class JournalImageUploadResponse
    {
        public string ImageUrl { get; set; } = string.Empty; // 用於插入 Tiptap 的 src
        public Guid Id { get; set; }
    }
    public class CursorPagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public string? NextCursor { get; set; }
        public bool HasNextPage => !string.IsNullOrEmpty(NextCursor);
    }
}