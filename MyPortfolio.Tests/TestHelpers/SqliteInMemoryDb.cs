using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Model;

namespace MyPortfolio.Tests.TestHelpers
{
    /// <summary>
    /// 建立 SQLite in-memory 的 MyDbContext。
    /// 連線必須保持開啟，資料庫才不會消失，因此由本類別持有並於 Dispose 時關閉。
    /// EnsureCreated 會套用 OnModelCreating 的結構與 HasData 種子資料（Categories、Tags）。
    /// </summary>
    public sealed class SqliteInMemoryDb : IDisposable
    {
        private readonly SqliteConnection _connection;

        public MyDbContext Context { get; }

        public SqliteInMemoryDb()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new MyDbContext(options);
            Context.Database.EnsureCreated();
        }

        /// <summary>建立第二個共用同一條連線的 Context，用來模擬不同 request 的讀取。</summary>
        public MyDbContext CreateFreshContext()
        {
            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new MyDbContext(options);
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
