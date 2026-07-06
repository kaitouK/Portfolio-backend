using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MyPortfolio.DTOs
{
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
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? NextCursor { get; set; }
        [JsonInclude]
        public bool HasNextPage => !string.IsNullOrEmpty(NextCursor);
    }
}