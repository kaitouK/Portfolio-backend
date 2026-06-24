namespace MyPortfolio.Utility
{
    public interface IImageValidator
    {
        bool IsValidImage(Stream stream, out string format);
    }

    public class ImageValidator : IImageValidator
    {
        private static readonly Dictionary<string, byte[]> Signatures = new()
        {
            { "JPG", new byte[] { 0xFF, 0xD8, 0xFF } },
         { "PNG", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
          { "WEBP", new byte[] { 0x52, 0x49, 0x46, 0x46, /* Size bytes */ 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 } },
        { "BMP", new byte[] { 0x42, 0x4D } }
        };

        public bool IsValidImage(Stream stream, out string format)
        {
            format = "Unknown";
            byte[] buffer = new byte[12];
            long originalPosition = stream.Position; // 紀錄原始位置

            if (stream.Length < buffer.Length)
            {
                stream.Position = originalPosition;
                return false;
            }

            stream.ReadExactly(buffer, 0, buffer.Length);
            stream.Position = originalPosition; // 重置指標，避免後續讀取失敗

            foreach (var sig in Signatures)
            {
                if (sig.Key == "WEBP")
                {
                    // 對於 WEBP，需要特殊處理，跳過中間的 4 個長度位元組
                    if (buffer.Take(4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) && // RIFF
                        buffer.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 })) // WEBP
                    {
                        format = sig.Key;
                        return true;
                    }
                }
                else if (buffer.Take(sig.Value.Length).SequenceEqual(sig.Value))
                {
                    format = sig.Key;
                    return true;
                }
            }
            return false;
        }
    }
}