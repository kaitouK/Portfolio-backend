using Microsoft.AspNetCore.Mvc;
using MyPortfolio.Common;
using System.Net;
namespace MyPortfolio.Controller
{
    //abstract避免被MVC當成Controller註冊
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        // 這裡可以放一些共用的功能，例如統一處理 ApiResponse 的回傳格式
        // 用protected方法只給子類別用，同時可以拿掉[NonAction]，因為這個方法不會被當作 API endpoint
        protected IActionResult ProcessApiResponse<T>(ApiResponse<T> response)
        => StatusCode(response.StatusCode, response);
        protected IActionResult ProcessApiResponse(ApiResponse response)
            => StatusCode(response.StatusCode, response);
        protected IActionResult ProcessApiResponse<T>(ServiceResult<T> result)
        {
            if (result.Success && result.StatusCode == HttpStatusCode.NoContent)
            {
                return NoContent();
            }
            var code = (int)result.StatusCode;
            var body = result.Success
                ? ApiResponse<T>.Ok(result.Data, result.Message, code)
                : ApiResponse<T>.Fail(result.Message, code);
            return StatusCode(code, body);
        }
        protected IActionResult ProcessApiResponse(ServiceResult result)
        {
            if (result.Success && result.StatusCode == HttpStatusCode.NoContent)
            {
                return NoContent();
            }
            var code = (int)result.StatusCode;
            var body = result.Success
                ? ApiResponse.Ok(result.Message, code)
                : ApiResponse.Fail(result.Message, code);
            return StatusCode(code, body);
        }
    }
}