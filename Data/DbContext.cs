using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Model.Entities;

namespace MyPortfolio.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }
        // 定義資料表
        public DbSet<Artwork> Artworks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ExternalStat> ExternalStats { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalImage> JournalImages { get; set; }
        public DbSet<JournalTag> JournalTags { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 設定 Artwork 的資料表結構和關聯
            modelBuilder.Entity<Artwork>(entity =>
            {
                entity.HasKey(at => at.ArtworkId); // 設定 ArtworkId 為主鍵
                entity.Property(at => at.Title).IsRequired().HasMaxLength(200);
                entity.Property(at => at.Description).HasMaxLength(1000);
                entity.Property(at => at.PixivId).IsRequired(false).HasMaxLength(50);
                // --- 新增：CompletionDate 屬性配置 ---
                entity.Property(at => at.CompletionDate)
                    .IsRequired(); // 時間軸的核心，設為必填

                entity.HasIndex(at => at.CompletionDate); // 為 CompletionDate 建立索引，提升排序和篩選效率
                entity.Property(at => at.IsGalleryVisible).HasDefaultValue(true);

                // 確保 PixivId 唯一
                entity.HasIndex(at => at.PixivId).IsUnique().HasFilter("\"PixivId\" IS NOT NULL"); // 只對非 null 的 PixivId 建立唯一索引
                // 設定與 Category 的一對多關係
                entity.HasOne(at => at.Category) // 一個作品有一個分類
                    .WithMany(c => c.Artworks) // 一個分類有多個作品
                    .HasForeignKey(at => at.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict); // 刪除分類時不刪除作品，但將 CategoryId 設為 null

                // 設定與 Tag 的多對多關係
                entity.HasMany(at => at.Tags) // 一個作品有多個標籤
                    .WithMany(t => t.Artworks) // 一個標籤有多個作品
                    .UsingEntity<Dictionary<string, object>>( // 使用匿名類型作為連接表
                        "ArtworkTag", // 連接表名稱
                        j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade), // 設定 Tag 的外鍵和刪除行為
                        j => j.HasOne<Artwork>().WithMany().HasForeignKey("ArtworkId").OnDelete(DeleteBehavior.Cascade) // 設定 Artwork 的外鍵和刪除行為
                    );
            });
            modelBuilder.Entity<Category>().HasData(
            // 預先定義一些類別資料，這些資料會在資料庫建立時自動插入 (DATA SEEDING)，這樣就不需要手動新增類別了
            new Category { CategoryId = 1, Name = "未分類" },
            new Category { CategoryId = 2, Name = "線稿" },
            new Category { CategoryId = 3, Name = "草圖" },
            new Category { CategoryId = 4, Name = "成圖" }
        );
            modelBuilder.Entity<Tag>().HasData(
            new Tag { TagId = 1, TagName = "正面", Type = TagType.Orientation },
            new Tag { TagId = 2, TagName = "1/4側面", Type = TagType.Orientation },
            new Tag { TagId = 3, TagName = "半側面", Type = TagType.Orientation },
            new Tag { TagId = 4, TagName = "3/4側面", Type = TagType.Orientation },
            new Tag { TagId = 5, TagName = "正側面", Type = TagType.Orientation }
        );

            // 設定 ExternalStat 與 Artwork 的一對一關係
            modelBuilder.Entity<ExternalStat>(entity =>
            {
                entity.HasKey(es => es.ArtworkId); // 設定 ArtworkId 為主鍵
                entity.HasOne(es => es.Artwork) // 一個統計數據對應一個作品
                    .WithOne(a => a.ExternalStat) // 一個作品有一個統計數據
                    .HasForeignKey<ExternalStat>(es => es.ArtworkId) // ExternalStat 的外鍵是 ArtworkId
                    .OnDelete(DeleteBehavior.Cascade); // 刪除作品時同時刪除統計數據
            });
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(j => j.Id);
                entity.Property(j => j.Title).IsRequired().HasMaxLength(200);
                entity.Property(j => j.Status).HasConversion<int>();

                // 建立索引以加速 Timeline 排序與草稿查詢
                entity.HasIndex(j => j.Status);
                entity.HasIndex(j => j.UpdatedAt);
            });

            modelBuilder.Entity<JournalImage>(entity =>
            {
                entity.HasKey(ji => ji.Id);
                entity.Property(ji => ji.ImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(ji => ji.BlobName).IsRequired().HasMaxLength(255);

                // 設定級聯刪除：當日誌被刪除時，自動刪除關聯的圖片紀錄
                entity.HasOne(ji => ji.JournalEntry)
                      .WithMany(j => j!.Images)
                      .HasForeignKey(ji => ji.JournalEntryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<JournalEntry>().ToTable("JournalEntries");
            modelBuilder.Entity<JournalImage>().ToTable("JournalImages");
            modelBuilder.Entity<JournalTag>().ToTable("JournalTags");
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.HasIndex(rt => rt.TokenHash).IsUnique(); // 每次都用雜湊查
                entity.HasIndex(rt => rt.FamilyId);             // 撤銷家族用
            });

        }
    }
}