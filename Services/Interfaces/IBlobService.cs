namespace MyPortfolio.Service.Interface
{
    public interface IBlobService
    {
        Task<string> UploadAsync(Stream stream, string blobName, string contentType);
        Task<bool> DeleteAsync(string blobName);
    }
}