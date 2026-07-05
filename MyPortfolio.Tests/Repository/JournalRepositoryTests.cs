using MyPortfolio.Model.Entities;
using MyPortfolio.Repository;
using MyPortfolio.Tests.TestHelpers;

namespace MyPortfolio.Tests.Repository
{
    public class JournalRepositoryTests : IDisposable
    {
        private readonly SqliteInMemoryDb _db;
        private readonly JournalRepository _repository;

        public JournalRepositoryTests()
        {
            _db = new SqliteInMemoryDb();
            _repository = new JournalRepository(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        private static JournalEntry MakeEntry(string title, JournalStatus status, DateTime? createdAt = null) => new()
        {
            Title = title,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };

        [Fact]
        public async Task GetOrCreateTag_NewName_CreatesTag()
        {
            var tag = await _repository.GetOrCreateTagAsync("速寫");
            await _repository.SaveChangesAsync();

            Assert.Equal("速寫", tag.Name);
            Assert.Single(_db.Context.JournalTags);
        }

        [Fact]
        public async Task GetOrCreateTag_TrimsWhitespace()
        {
            var tag = await _repository.GetOrCreateTagAsync("  速寫  ");
            await _repository.SaveChangesAsync();

            Assert.Equal("速寫", tag.Name);
        }

        [Fact]
        public async Task GetOrCreateTag_ExistingName_ReturnsSameTagWithoutDuplicate()
        {
            var first = await _repository.GetOrCreateTagAsync("Sketch");
            await _repository.SaveChangesAsync();

            var second = await _repository.GetOrCreateTagAsync("Sketch");
            await _repository.SaveChangesAsync();

            Assert.Equal(first.Id, second.Id);
            Assert.Single(_db.Context.JournalTags);
        }

        [Fact]
        public async Task GetOrCreateTag_MatchIsCaseInsensitive()
        {
            var first = await _repository.GetOrCreateTagAsync("Sketch");
            await _repository.SaveChangesAsync();

            var second = await _repository.GetOrCreateTagAsync("sketch");
            await _repository.SaveChangesAsync();

            Assert.Equal(first.Id, second.Id);
            Assert.Single(_db.Context.JournalTags);
        }

        [Fact]
        public async Task GetActiveDraft_ReturnsOnlyDraftStatus()
        {
            await _repository.AddAsync(MakeEntry("published", JournalStatus.Published));
            await _repository.AddAsync(MakeEntry("draft", JournalStatus.Draft));
            await _repository.SaveChangesAsync();

            var draft = await _repository.GetActiveDraftAsync();

            Assert.NotNull(draft);
            Assert.Equal("draft", draft.Title);
        }

        [Fact]
        public async Task GetActiveDraft_NoDraft_ReturnsNull()
        {
            await _repository.AddAsync(MakeEntry("published", JournalStatus.Published));
            await _repository.SaveChangesAsync();

            var draft = await _repository.GetActiveDraftAsync();

            Assert.Null(draft);
        }

        [Fact]
        public async Task GetPublishedList_ExcludesDraftsAndOrdersByCreatedAtDescending()
        {
            var now = DateTime.UtcNow;
            await _repository.AddAsync(MakeEntry("old-post", JournalStatus.Published, now.AddDays(-2)));
            await _repository.AddAsync(MakeEntry("new-post", JournalStatus.Published, now));
            await _repository.AddAsync(MakeEntry("draft", JournalStatus.Draft, now.AddDays(1)));
            await _repository.SaveChangesAsync();

            var list = await _repository.GetPublishedListAsync();

            Assert.Equal(new[] { "new-post", "old-post" }, list.Select(j => j.Title));
        }
    }
}
