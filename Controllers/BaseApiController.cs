using MyPortfolio.DTOs;
using Microsoft.AspNetCore.Mvc;
using MyPortfolio.Model;
using MyPortfolio.Service.Interface;
namespace MyPortfolio.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        // 這裡可以放一些共用的功能，例如統一處理 ApiResponse 的回傳格式
        [NonAction]
        public IActionResult ProcessApiResponse<T>(ApiResponse<T> response)
        => StatusCode(response.StatusCode, response);

        [NonAction]
        public IActionResult ProcessApiResponse(ApiResponse response)
            => StatusCode(response.StatusCode, response);
        [NonAction]
        public IActionResult ProcessApiResponse<T>(ServiceResult<T> serviceResult)
        {
            var apiResponse = serviceResult.Success
                ? ApiResponse<T>.Ok(serviceResult.Data, serviceResult.Message)
                : ApiResponse<T>.Fail(serviceResult.Message, (int)serviceResult.StatusCode);
            return StatusCode(apiResponse.StatusCode, apiResponse);
        }
        [NonAction]
        public IActionResult ProcessApiResponse(ServiceResult serviceResult)
        {
            if (serviceResult.Success && serviceResult.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return NoContent();
            }
            var apiResponse = serviceResult.Success
                ? ApiResponse.Ok(serviceResult.Message)
                : ApiResponse.Fail(serviceResult.Message, (int)serviceResult.StatusCode);
            return StatusCode(apiResponse.StatusCode, apiResponse);
        }
    }
}