using Microsoft.Extensions.Logging.Abstractions;
using MyPortfolio.Model.Entities;
using MyPortfolio.Repository;
using MyPortfolio.Service;
using MyPortfolio.Tests.TestHelpers;
using SkiaSharp;

namespace MyPortfolio.Tests.Service
{
    public class ArtworkServiceTests : IDisposable
    {
        private readonly SqliteInMemoryDb _db;
        private readonly FakeBlobService _blob = new();
        private readonly ArtworkService _service;

        public ArtworkServiceTests()
        {
            _db = new SqliteInMemoryDb();
            _service = new ArtworkService(new ArtworkRepository(_db.Context), NullLogger<ArtworkService>.Instance, _blob);
        }

        public void Dispose() => _db.Dispose();

        private static Artwork MakeArtwork(string title, DateTime completionDate, string? imageUrl = null) => new()
        {
            Title = title,
            CategoryId = 1,
            CompletionDate = completionDate,
            CreatedAt = DateTime.UtcNow,
            ImageUrl = imageUrl,
        };

        // 依序建立 day1 ~ dayN（日期越大排越前面）
        private async Task<List<Artwork>> SeedDaysAsync(int count)
        {
            var artworks = Enumerable.Range(1, count)
                .Select(i => MakeArtwork($"day{i}", new DateTime(2026, 1, i)))
                .ToList();
            _db.Context.Artworks.AddRange(artworks);
            await _db.Context.SaveChangesAsync();
            return artworks;
        }

        private static string CursorOf(Artwork artwork) =>
            $"{artwork.CompletionDate:yyyy-MM-ddTHH:mm:ss.fff}_{artwork.ArtworkId}";

        private static byte[] CreatePng(int width, int height)
        {
            using var bitmap = new SKBitmap(width, height);
            bitmap.Erase(SKColors.CornflowerBlue);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        [Fact]
        public async Task GetPaged_UntilIdDeeperThanLimit_LoadsThroughTarget()
        {
            var days = await SeedDaysAsync(6);
            var target = days[1]; // day2：排序 day6, day5, day4, day3, day2, day1 的第 5 筆

            var result = await _service.GetPagedArtworksAsync(2, null, target.ArtworkId);

            Assert.True(result.Success);
            Assert.Equal(new[] { "day6", "day5", "day4", "day3", "day2" }, result.Data!.Data.Select(a => a.Title));
            Assert.Equal(CursorOf(target), result.Data.NextCursor);
            Assert.True(result.Data.HasNextPage);
        }

        [Fact]
        public async Task GetPaged_UntilIdContinuesFromCursor()
        {
            var days = await SeedDaysAsync(6);
            var cursor = CursorOf(days[5]); // 已載入 day6

            var result = await _service.GetPagedArtworksAsync(1, cursor, days[2].ArtworkId);

            Assert.Equal(new[] { "day5", "day4", "day3" }, result.Data!.Data.Select(a => a.Title));
        }

        [Fact]
        public async Task GetPaged_UntilIdShallowerThanLimit_StillReturnsFullPage()
        {
            var days = await SeedDaysAsync(6);

            var result = await _service.GetPagedArtworksAsync(4, null, days[5].ArtworkId);

            Assert.Equal(4, result.Data!.Data.Count());
        }

        [Fact]
        public async Task GetPaged_UntilIdMissing_ReturnsNormalPage()
        {
            await SeedDaysAsync(6);

            var result = await _service.GetPagedArtworksAsync(2, null, 999);

            Assert.Equal(new[] { "day6", "day5" }, result.Data!.Data.Select(a => a.Title));
        }

        [Fact]
        public async Task GetArtworkIndex_ReturnsAllVisibleInOrder()
        {
            await SeedDaysAsync(3);

            var result = await _service.GetArtworkIndexAsync();

            Assert.True(result.Success);
            Assert.Equal(new[] { "day3", "day2", "day1" }, result.Data!.Select(i => i.Title));
        }

        [Fact]
        public async Task BackfillImages_FillsSizeAndDisplayAndSkipsBrokenOnes()
        {
            _blob.Blobs["large.webp"] = CreatePng(2800, 1000);
            _blob.Blobs["small.webp"] = CreatePng(800, 600);
            var large = MakeArtwork("large", new DateTime(2026, 1, 1), "https://fake.blob/images/large.webp");
            var broken = MakeArtwork("broken", new DateTime(2026, 1, 2), "https://fake.blob/images/missing.webp");
            var small = MakeArtwork("small", new DateTime(2026, 1, 3), "https://fake.blob/images/small.webp");
            _db.Context.Artworks.AddRange(large, broken, small);
            await _db.Context.SaveChangesAsync();

            var first = await _service.BackfillImagesAsync(0, 2);

            Assert.True(first.Success);
            Assert.Equal(1, first.Data!.Processed);
            Assert.Equal(1, first.Data.Failed);
            Assert.Equal(broken.ArtworkId, first.Data.LastId); // 失敗的也算處理過，游標往前推
            Assert.True(first.Data.HasMore);

            var second = await _service.BackfillImagesAsync(first.Data.LastId, 2);

            Assert.Equal(1, second.Data!.Processed);
            Assert.False(second.Data.HasMore);

            using var verify = _db.CreateFreshContext();
            var savedLarge = verify.Artworks.Single(a => a.ArtworkId == large.ArtworkId);
            var savedSmall = verify.Artworks.Single(a => a.ArtworkId == small.ArtworkId);
            var savedBroken = verify.Artworks.Single(a => a.ArtworkId == broken.ArtworkId);

            Assert.Equal((2800, 1000), (savedLarge.ImageWidth, savedLarge.ImageHeight));
            Assert.Equal("https://fake.blob/images/large_display.webp", savedLarge.DisplayUrl);
            using (var display = SKBitmap.Decode(_blob.Blobs["large_display.webp"]))
            {
                Assert.Equal(1400, display.Width);
                Assert.Equal(500, display.Height);
            }

            Assert.Equal((800, 600), (savedSmall.ImageWidth, savedSmall.ImageHeight));
            Assert.Null(savedSmall.DisplayUrl); // 原圖夠小，不產生顯示圖
            Assert.Null(savedBroken.ImageWidth);
        }
    }
}
