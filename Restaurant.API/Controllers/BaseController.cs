using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NEEFRA.API.Helpers;
using NEEFRA.Core.DTO.Service;
using System.Security.Claims;

namespace Restaurant.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class BaseController : ControllerBase
    {
        protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        protected string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        protected IActionResult HandleResult<T>(ServiceResult<T> result)
        {
       
            if (result.IsSuccess)
                return ApiResponseHelper.Success(result.Data, result.Message);

            return result.ErrorType switch
            {
                "NotFound" => ApiResponseHelper.NotFound(result.Message),
                "Forbidden" => Forbid(result.Message!),
                "Unauthorized" => ApiResponseHelper.Unauthorized(result.Message),
                _ => ApiResponseHelper.BadRequest(result.Message)
            };
        }
    }
}
