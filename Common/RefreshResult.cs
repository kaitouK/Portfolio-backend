namespace MyPortfolio.Common
{
    public class RefreshResult
    {
        public bool Success { get; init; }
        public string? NewAccessToken { get; init; }
        public string? Email { get; init; }
        public string? NewRefreshPlainToken { get; init; }
        public DateTime RefreshExpiresAtUtc { get; init; }
        public static RefreshResult Fail() => new() { Success = false };
    }
}