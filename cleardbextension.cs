using Microsoft.EntityFrameworkCore;

public static class DbContextExtensions
{
    public static void ClearJournalTables(this DbContext context)
    {
        // 1. 開啟資料庫事務 (Transaction)，確保三個表要嘛一起清空，要嘛都不清空
        using var transaction = context.Database.BeginTransaction();
        try
        {
            // 2. 關閉 SQLite 的外鍵約束檢查
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");

            // 3. 清空個別資料表
            context.Database.ExecuteSqlRaw("DELETE FROM JournalEntries;");
            context.Database.ExecuteSqlRaw("DELETE FROM JournalImages;");
            context.Database.ExecuteSqlRaw("DELETE FROM JournalTags;");

            // 4. 重設自動遞增的 Primary Key 計數器
            context.Database.ExecuteSqlRaw("DELETE FROM sqlite_sequence WHERE name IN ('JournalEntries', 'JournalImages', 'JournalTags');");

            // 5. 重新開啟外鍵約束
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");

            // 6. 提交事務
            transaction.Commit();
        }
        catch (Exception)
        {
            // 發生錯誤時回復上一動
            transaction.Rollback();
            throw;
        }
    }
}