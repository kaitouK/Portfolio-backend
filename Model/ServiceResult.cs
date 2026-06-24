using System.Net;
namespace MyPortfolio.Model
{
    /// <summary>
    /// 服務層的結果封裝類別，用於統一表示服務操作的成功與否、相關訊息以及狀態碼。這個類別可以幫助我們在控制器中更方便地處理服務層的回應，並根據結果來決定回傳給客戶端的 HTTP 狀態碼和訊息。
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public HttpStatusCode StatusCode { get; init; }

        public static ServiceResult Ok(string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK) => new()
        {
            Success = true,
            Message = message ?? "Success",
            StatusCode = statusCode
        };

        public static ServiceResult Fail(string? message = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest) => new()
        {
            Success = false,
            Message = message ?? "An unexpected error occurred",
            StatusCode = statusCode
        };
    }
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        public static ServiceResult<T> Ok(T data, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK) => new()
        {
            Success = true,
            Data = data,
            Message = message ?? "Success",
            StatusCode = statusCode
        };

        public new static ServiceResult<T> Fail(string? message = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest) => new()
        {
            Success = false,
            Data = default,
            Message = message ?? "An unexpected error occurred",
            StatusCode = statusCode
        };
    }
}