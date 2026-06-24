using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPortfolio.Model.Entities
{
    public enum JournalStatus
    {
        Draft = 0,
        Published = 1,
        Archived = 2
    }
    public class JournalEntry
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string ContentJson { get; set; } = "{}"; // 內含 <img> 網址，供 Tiptap 還原

        [Required]
        public string ContentHtml { get; set; } = string.Empty; // 內含 <img> 網址，供前台渲染

        public virtual ICollection<JournalTag> JournalTags { get; set; } = new List<JournalTag>();

        public JournalStatus Status { get; set; } = JournalStatus.Draft;

        public Guid? UserId { get; set; }//預留多用戶擴充

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // 導覽屬性：一篇日誌可以包含多張圖片
        public virtual ICollection<JournalImage> Images { get; set; } = new List<JournalImage>();
    }
    // 標籤主表
    public class JournalTag
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)] // 標籤名字通常不用太長
        public string Name { get; set; } = string.Empty;

        // 反向導覽屬性：一個標籤也可以屬於多篇文章
        public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    }
    /// <summary>
    /// 用於追蹤哪些圖片檔案屬於哪篇日誌。清理孤兒圖片（Orphaned Images）（例如：使用者在編輯時上傳了圖片，但隨後在編輯器中刪除，或直接捨棄草稿）。
    /// </summary>
    public class JournalImage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // 外鍵：對應到日誌主表
        [Required]
        public Guid JournalEntryId { get; set; }

        [ForeignKey(nameof(JournalEntryId))]
        public virtual JournalEntry JournalEntry { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty; // 儲存該圖片的完整網址

        [Required]
        [StringLength(255)]
        public string LocalFilePath { get; set; } = string.Empty; // 儲存後端硬碟的實際路徑 (刪除檔案時需要)

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}