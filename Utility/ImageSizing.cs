namespace MyPortfolio.Utility
{
    /// <summary>
    /// 縮圖與顯示圖共用的尺寸計算：等比縮放到上限內，比上限小時不放大。
    /// </summary>
    public static class ImageSizing
    {
        public static (int Width, int Height) ScaleToFit(int width, int height, int maxWidth, int maxHeight)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "圖片寬高必須大於 0");

            double ratio = Math.Min(1d, Math.Min((double)maxWidth / width, (double)maxHeight / height));
            if (ratio >= 1d) return (width, height);

            // 至少保留 1px，避免極端長寬比縮成 0
            return (Math.Max(1, (int)Math.Round(width * ratio)), Math.Max(1, (int)Math.Round(height * ratio)));
        }
    }
}
