using MyPortfolio.Service.Interface;

namespace MyPortfolio.Tests.TestHelpers
{
    /// <summary>
    /// 記憶體版的 IBlobService：上傳存進字典、下載從字典讀，找不到就丟例外（模擬 blob 不存在）。
    /// </summary>
    public sealed class FakeBlobService : IBlobService
    {
        public Dictionary<string, byte[]> Blobs { get; } = new();

        public async Task<string> UploadAsync(Stream stream, string blobName, string contentType)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            Blobs[blobName] = memoryStream.ToArray();
            return $"https://fake.blob/images/{blobName}";
        }

        public Task<bool> DeleteAsync(string blobName) => Task.FromResult(Blobs.Remove(blobName));

        public Task<Stream> DownloadAsync(string blobName) =>
            Blobs.TryGetValue(blobName, out var bytes)
                ? Task.FromResult<Stream>(new MemoryStream(bytes))
                : throw new FileNotFoundException("blob 不存在", blobName);
    }
}
