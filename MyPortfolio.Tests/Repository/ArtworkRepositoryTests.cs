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
        public async Task GetIndex_OrdersLikePagedListAndExcludesHidden()
        {
            var day = new DateTime(2026, 1, 5);
            await SeedAsync(
                MakeArtwork("older", new DateTime(2026, 1, 1)),
                MakeArtwork("sameDayFirst", day),
                MakeArtwork("hidden", new DateTime(2026, 1, 9), visible: false),
                MakeArtwork("sameDaySecond", day));

            var index = await _repository.GetIndexAsync();
            var (paged, _) = await _repository.GetPagedArtworksAsync(10, null, null);

            Assert.Equal(new[] { "sameDaySecond", "sameDayFirst", "older" }, index.Select(i => i.Title));
            Assert.Equal(paged.Select(a => a.ArtworkId), index.Select(i => i.ArtworkId));
        }

        [Fact]
        public async Task CountThrough_WithoutCursor_CountsFromFirstItemToTargetInclusive()
        {
            var target = MakeArtwork("day2", new DateTime(2026, 1, 2));
            await SeedAsync(
                MakeArtwork("day1", new DateTime(2026, 1, 1)),
                target,
                MakeArtwork("day3", new DateTime(2026, 1, 3)),
                MakeArtwork("day4", new DateTime(2026, 1, 4)));

            var count = await _repository.CountThroughAsync(null, null, target.ArtworkId);

            Assert.Equal(3, count); // day4, day3, day2
        }

        [Fact]
        public async Task CountThrough_WithCursor_CountsOnlyItemsAfterCursor()
        {
            var day4 = MakeArtwork("day4", new DateTime(2026, 1, 4));
            var day1 = MakeArtwork("day1", new DateTime(2026, 1, 1));
            await SeedAsync(
                day1,
                MakeArtwork("day2", new DateTime(2026, 1, 2)),
                MakeArtwork("day3", new DateTime(2026, 1, 3)),
                day4);

            // 已載入 day4，游標指向它 → 剩 day3, day2, day1
            var count = await _repository.CountThroughAsync(day4.CompletionDate, day4.ArtworkId, day1.ArtworkId);

            Assert.Equal(3, count);
        }

        [Fact]
        public async Task CountThrough_SameDate_UsesIdAsTieBreaker()
        {
            var day = new DateTime(2026, 1, 5);
            var first = MakeArtwork("first", day);
            var second = MakeArtwork("second", day);
            var third = MakeArtwork("third", day);
            await SeedAsync(first, second, third);

            // 同日期依 Id 遞減：third, second, first
            var count = await _repository.CountThroughAsync(null, null, second.ArtworkId);

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task CountThrough_HiddenOrMissingTarget_ReturnsNull()
        {
            var hidden = MakeArtwork("hidden", new DateTime(2026, 1, 1), visible: false);
            await SeedAsync(hidden, MakeArtwork("visible", new DateTime(2026, 1, 2)));

            Assert.Null(await _repository.CountThroughAsync(null, null, hidden.ArtworkId));
            Assert.Null(await _repository.CountThroughAsync(null, null, 999));
        }

        [Fact]
        public async Task GetMissingImageSize_RespectsAfterIdTakeAndIncludesHidden()
        {
            var a = MakeArtwork("a", new DateTime(2026, 1, 1));
            var b = MakeArtwork("b", new DateTime(2026, 1, 2), visible: false);
            var c = MakeArtwork("c", new DateTime(2026, 1, 3));
            var done = MakeArtwork("done", new DateTime(2026, 1, 4));
            done.ImageWidth = 100;
            done.ImageHeight = 100;
            var d = MakeArtwork("d", new DateTime(2026, 1, 5));
            await SeedAsync(a, b, c, done, d);

            var batch = await _repository.GetMissingImageSizeAsync(a.ArtworkId, 2);

            Assert.Equal(new[] { "b", "c" }, batch.Select(x => x.Title));

            var rest = await _repository.GetMissingImageSizeAsync(c.ArtworkId, 10);
            Assert.Equal(new[] { "d" }, rest.Select(x => x.Title)); // 已有寬高的 done 被略過
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
            artwork.ImageUrl = "https://blob/image.webp";
            artwork.ThumbnailUrl = "https://blob/image_thumb.webp";
            artwork.DisplayUrl = "https://blob/image_display.webp";
            artwork.ImageWidth = 2800;
            artwork.ImageHeight = 4000;
            await SeedAsync(artwork);

            var dto = await _repository.GetDtoByIdAsync(artwork.ArtworkId);

            Assert.NotNull(dto);
            Assert.Equal("title", dto.Title);
            Assert.Equal("desc", dto.Description);
            Assert.Equal(1, dto.CategoryId);
            Assert.Equal("https://blob/image.webp", dto.ImageUrl);
            Assert.Equal("https://blob/image_thumb.webp", dto.ThumbnailUrl);
            Assert.Equal("https://blob/image_display.webp", dto.DisplayUrl);
            Assert.Equal(2800, dto.ImageWidth);
            Assert.Equal(4000, dto.ImageHeight);
        }
    }
}
