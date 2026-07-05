using MyPortfolio.Utility;

namespace MyPortfolio.Tests.Utility
{
    public class ImageValidatorTests
    {
        private readonly ImageValidator _validator = new();

        // 建立一個至少 12 bytes 的串流，前面放指定的 magic number，後面補 0
        private static MemoryStream CreateStream(byte[] header, int totalLength = 16)
        {
            var bytes = new byte[Math.Max(totalLength, header.Length)];
            header.CopyTo(bytes, 0);
            return new MemoryStream(bytes);
        }

        [Fact]
        public void IsValidImage_PngMagicNumber_ReturnsTrueWithPngFormat()
        {
            using var stream = CreateStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            var result = _validator.IsValidImage(stream, out var format);

            Assert.True(result);
            Assert.Equal("PNG", format);
        }

        [Fact]
        public void IsValidImage_JpegMagicNumber_ReturnsTrueWithJpgFormat()
        {
            using var stream = CreateStream(new byte[] { 0xFF, 0xD8, 0xFF });

            var result = _validator.IsValidImage(stream, out var format);

            Assert.True(result);
            Assert.Equal("JPG", format);
        }

        [Fact]
        public void IsValidImage_WebpMagicNumber_ReturnsTrueWithWebpFormat()
        {
            // RIFF + 任意長度位元組 + WEBP
            using var stream = CreateStream(new byte[]
            {
                0x52, 0x49, 0x46, 0x46, // RIFF
                0x12, 0x34, 0x56, 0x78, // 檔案長度（任意值，不影響判定）
                0x57, 0x45, 0x42, 0x50  // WEBP
            });

            var result = _validator.IsValidImage(stream, out var format);

            Assert.True(result);
            Assert.Equal("WEBP", format);
        }

        [Fact]
        public void IsValidImage_RiffWithoutWebpTag_ReturnsFalse()
        {
            // RIFF 開頭但不是 WEBP（例如 WAV 檔）
            using var stream = CreateStream(new byte[]
            {
                0x52, 0x49, 0x46, 0x46,
                0x12, 0x34, 0x56, 0x78,
                0x57, 0x41, 0x56, 0x45  // WAVE
            });

            var result = _validator.IsValidImage(stream, out var format);

            Assert.False(result);
            Assert.Equal("Unknown", format);
        }

        [Fact]
        public void IsValidImage_PlainTextContent_ReturnsFalse()
        {
            // 偽裝成圖片的文字檔（改副檔名攻擊）
            using var stream = new MemoryStream("<script>alert(1)</script>"u8.ToArray());

            var result = _validator.IsValidImage(stream, out var format);

            Assert.False(result);
            Assert.Equal("Unknown", format);
        }

        [Fact]
        public void IsValidImage_StreamShorterThanHeader_ReturnsFalse()
        {
            using var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });

            var result = _validator.IsValidImage(stream, out _);

            Assert.False(result);
        }

        [Fact]
        public void IsValidImage_AfterValidation_StreamPositionIsRestored()
        {
            // 驗證後必須重置串流位置，否則後續的 SkiaSharp 解碼會失敗
            using var stream = CreateStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            _validator.IsValidImage(stream, out _);

            Assert.Equal(0, stream.Position);
        }
    }
}
