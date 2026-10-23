namespace MyPortfolio.Common
{
    /// <summary>
    /// 基本的 API 回應格式(不包含資料）
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; } 
        public string? Message { get; set; }
        public int StatusCode { get; set; } = 200; 

        public static ApiResponse Ok(string? message = "Success",int statusCode = 200) => new()
        {
            Success = true,
            Message = message ?? "Success",
            StatusCode = statusCode 
        };
        public static ApiResponse Created(string? message = "Resource created successfully", int statusCode = 201) => new()
        {
            Success = true,
            Message = message ?? "Resource created successfully",
            StatusCode = statusCode 
        };
        
        public static ApiResponse Fail(string? message, int statusCode=400) => new()
        {
            Success = false,
            Message = message?? "An unexpected error occurred",
            StatusCode = statusCode
        };
    }
    /// <summary>
    /// 包含資料的 API 回應格式，泛型 T 代表資料的類型，可以是任何類型（例如：單一物件、列表等）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T>: ApiResponse
    {
        public T? Data { get; set; } // The data returned by the API, can be null if not applicable

        // Constructor to initialize the ApiResponse with default values
        public static ApiResponse<T> Ok(T? data, string? message = null,int statusCode = 200) => new()
        {
            Success = true,
            Data = data,
            Message = message ?? "Success",
            StatusCode = statusCode // Default status code for a successful response
        };
        public static ApiResponse<T> Created(T? data, string? message = null, int statusCode = 201) => new()
        {
            Success = true,
            Data = data,
            Message = message ?? "Resource created successfully",
            StatusCode = statusCode // Default status code for a resource creation
        };
        public new static ApiResponse<T> Fail(string? message=null, int statusCode=400) => new()
        {
            Success = false,
            Data = default,
            Message = message ?? "An unexpected error occurred",
            StatusCode = statusCode // Default status code for a failed response
        };
    }
}