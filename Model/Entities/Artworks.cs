using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPortfolio.Model.Entities
{
    //Artworks主表，包含作品的基本信息和與Pixiv的對應ID
    public class Artwork
    {
        public int ArtworkId { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        [MaxLength(50)]
        public string? PixivId { get; set; }
        public DateTime CreatedAt { get; set; }
        //新增：作品完成日期
        public DateTime CompletionDate { get; set; }
        public bool IsGalleryVisible { get; set; } = true;

        // 外鍵與導航屬性
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // 多對多關係
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        // 一對一或一對多統計數據
        public ExternalStat? ExternalStat { get; set; }
    }
    // 用來儲存作品的分類和標籤。
    public class Category
    {
        public int CategoryId { get; set; }
        [Required, MaxLength(100)]
        public required string Name { get; set; }
        // 建立反向導覽，方便從分類找作品
        public ICollection<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
    // 用來儲存作品與標籤的多對多關係。
    public class Tag
    {
        public int TagId { get; set; }
        [Required, MaxLength(100)]

        public required string TagName { get; set; }
        [Required]
        public TagType Type { get; set; } = TagType.General;
        // 建立反向導覽，方便從標籤找作品
        public ICollection<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
    // 用來儲存作品與標籤的多對多關係，(如果需要更複雜的關聯屬性可以使用這個實體，但目前簡化為直接在 Artwork 和 Tag 中建立多對多關係)
    public enum TagType
    {
        General = 0,      // 一般標籤 (如：二次元、厚塗、原創)
        Orientation = 1   // 人物朝向標籤
    }
    public class ExternalStat
    {
        //public int StatId { get; set; } 如果需要獨立的主鍵可以加上，但目前設計為與 Artwork 一對一，使用 ArtworkId 作為主鍵和外鍵
        [Key, ForeignKey("Artwork")]
        public required int ArtworkId { get; set; }// 外鍵
        public Artwork? Artwork { get; set; } // 導覽屬性
        public int PixivViews { get; set; }
        public int PixivLikes { get; set; }
        public int PixivBookmarks { get; set; }
        public required DateTime LastSyncedAt { get; set; }
    }

}