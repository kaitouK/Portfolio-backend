using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    public class ArtworkUpdateRequest
    {
        public int ArtworkId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? CompletionDate { get; set; } // 作品完成日期
    }
}