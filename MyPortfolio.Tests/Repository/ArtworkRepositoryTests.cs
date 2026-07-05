using MyPortfolio.Model.Entities;
using MyPortfolio.Repository;
using MyPortfolio.Tests.TestHelpers;

namespace MyPortfolio.Tests.Repository
{
    public class ArtworkRepositoryTests : IDisposable
    {
        private readonly SqliteInMemoryDb _db;
        private readonly ArtworkRepository _repository;

        public ArtworkRepositoryTests()
        {
            _db = new SqliteInMemoryDb();
            _repository = new ArtworkRepository(_db.Context);
        }

        public void Dispose() => _db.Dispose();

        // CategoryId = 1（未分類）來自 OnModelCreating 的種子資料
        private static Artwork MakeArtwork(string title, DateTime completionDate, bool visible = true) => new()
        {
            Title = title,
            CategoryId = 1,
            CompletionDate = completionDate,
            CreatedAt = DateTime.UtcNow,
            IsGalleryVisible = visible,
        };

        private async Task SeedAsync(params Artwork[] artworks)
        {
            _db.Context.Artworks.AddRange(artworks);
            await _db.Context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetPagedArtworks_OrdersByCompletionDateThenIdDescending()
        {
            await SeedAsync(
                MakeArtwork("oldest", new DateTime(2026, 1, 1)),
                MakeArtwork("newest", new DateTime(2026, 1, 3)),
                MakeArtwork("middle", new DateTime(2026, 1, 2)));

            var (data, _) = await _repository.GetPagedArtworksAsync(10, null, null);

            Assert.Equal(new[] { "newest", "middle", "oldest" }, data.Select(a => a.Title));
        }

        [Fact]
        public async Task GetPagedArtworks_ExcludesHiddenArtworks()
        {
            await SeedAsync(
                MakeArtwork("visible", new DateTime(2026, 1, 1)),
                MakeArtwork("hidden", new DateTime(2026, 1, 2), visible: false));

            var (data, _) = await _repository.GetPagedArtworksAsync(10, null, null);

            var titles = data.Select(a => a.Title).ToList();
            Assert.Contains("visible", titles);
            Assert.DoesNotContain("hidden", titles);
        }

        [Fact]
        public async Task GetPagedArtworks_HasNextPage_TrueWhenMoreThanLimit()
        {
            await SeedAsync(
                MakeArtwork("a", new DateTime(2026, 1, 1)),
                MakeArtwork("b", new DateTime(2026, 1, 2)),
                MakeArtwork("c", new DateTime(2026, 1, 3)));

            var (data, hasNextPage) = await _repository.GetPagedArtworksAsync(2, null, null);

            Assert.True(hasNextPage);
            Assert.Equal(2, data.Count()); // 多抓的那一筆必須被移除
        }

        [Fact]
        public async Task GetPagedArtworks_HasNextPage_FalseWhenExactlyLimit()
        {
            await SeedAsync(
                MakeArtwork("a", new DateTime(2026, 1, 1)),
                MakeArtwork("b", new DateTime(2026, 1, 2)));

            var (data, hasNextPage) = await _repository.GetPagedArtworksAsync(2, null, null);

            Assert.False(hasNextPage);
            Assert.Equal(2, data.Count());
        }

        [Fact]
        public async Task GetPagedArtworks_CursorSkipsAlreadySeenItems()
        {
            var day = new DateTime(2026, 1, 5);
            await SeedAsync(
                MakeArtwork("first", day),
                MakeArtwork("second", day),
                MakeArtwork("third", day));

            // 第一頁：同日期以 Id 遞減 → third, second
            var (page1, hasNext1) = await _repository.GetPagedArtworksAsync(2, null, null);
            Assert.True(hasNext1);
            Assert.Equal(new[] { "third", "second" }, page1.Select(a => a.Title));

            // 用第一頁最後一筆當游標 → 剩下 first
            var last = page1.Last();
            var (page2, hasNext2) = await _repository.GetPagedArtworksAsync(2, last.CompletionDate, last.ArtworkId);

            Assert.False(hasNext2);
            Assert.Equal(new[] { "first" }, page2.Select(a => a.Title));
        }

        [Fact]
        public async Task GetPagedArtworks_CursorAcrossDifferentDates()
        {
            await SeedAsync(
                MakeArtwork("day1", new DateTime(2026, 1, 1)),
                MakeArtwork("day2", new DateTime(2026, 1, 2)),
                MakeArtwork("day3", new DateTime(2026, 1, 3)));

            var (page1, _) = await _repository.GetPagedArtworksAsync(1, null, null);
            var last = page1.Single();
            Assert.Equal("day3", last.Title);

            var (page2, _) = await _repository.GetPagedArtworksAsync(1, last.CompletionDate, last.ArtworkId);

            Assert.Equal("day2", page2.Single().Title);
        }

        [Fact]
        public async Task GetDtoById_NotFound_ReturnsNull()
        {
            var dto = await _repository.GetDtoByIdAsync(999);

            Assert.Null(dto);
        }

        [Fact]
        public async Task GetDtoById_MapsEntityToDto()
        {
            var artwork = MakeArtwork("title", new DateTime(2026, 1, 1));
            artwork.Description = "desc";
            await SeedAsync(artwork);

            var dto = await _repository.GetDtoByIdAsync(artwork.ArtworkId);

            Assert.NotNull(dto);
            Assert.Equal("title", dto.Title);
            Assert.Equal("desc", dto.Description);
            Assert.Equal(1, dto.CategoryId);
        }
    }
}
