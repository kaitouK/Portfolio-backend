using MyPortfolio.Utility;

namespace MyPortfolio.Tests.Utility
{
    public class ImageSizingTests
    {
        [Fact]
        public void ScaleToFit_LandscapeLargerThanBox_LimitedByWidth()
        {
            var (width, height) = ImageSizing.ScaleToFit(3000, 2000, 300, 300);

            Assert.Equal(300, width);
            Assert.Equal(200, height);
        }

        [Fact]
        public void ScaleToFit_PortraitLargerThanBox_LimitedByHeight()
        {
            var (width, height) = ImageSizing.ScaleToFit(2000, 3000, 300, 300);

            Assert.Equal(200, width);
            Assert.Equal(300, height);
        }

        [Fact]
        public void ScaleToFit_SmallerThanBox_DoesNotUpscale()
        {
            var (width, height) = ImageSizing.ScaleToFit(120, 80, 300, 300);

            Assert.Equal(120, width);
            Assert.Equal(80, height);
        }

        [Fact]
        public void ScaleToFit_ExactlyAtLimit_ReturnsSameSize()
        {
            var (width, height) = ImageSizing.ScaleToFit(1400, 5000, 1400, int.MaxValue);

            Assert.Equal(1400, width);
            Assert.Equal(5000, height);
        }

        [Fact]
        public void ScaleToFit_DisplayImage_HeightUnbounded()
        {
            // 顯示圖只限制寬度，直式長圖照比例縮
            var (width, height) = ImageSizing.ScaleToFit(2800, 8000, 1400, int.MaxValue);

            Assert.Equal(1400, width);
            Assert.Equal(4000, height);
        }

        [Fact]
        public void ScaleToFit_ExtremeAspectRatio_KeepsAtLeastOnePixel()
        {
            var (width, height) = ImageSizing.ScaleToFit(10000, 2, 300, 300);

            Assert.Equal(300, width);
            Assert.Equal(1, height);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        [InlineData(-1, 100)]
        public void ScaleToFit_NonPositiveSize_Throws(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ImageSizing.ScaleToFit(width, height, 300, 300));
        }
    }
}
