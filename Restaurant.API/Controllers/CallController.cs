using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NEEFRA.Core;
using Realtima_Chat_project.DTO;
using Restaurant.API.Controllers;
using Restaurant.Core.DTO.Chat;

namespace Realtima_Chat_project.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class CallController : BaseController
    {
        private readonly ICallService _callService;

        public CallController(ICallService callService)
        {
            _callService = callService;
        }

     

        [HttpPost("SaveCallLog")]
        public async Task<IActionResult> SaveCallLog([FromBody] CallDTO dto)
        {
            var result = await _callService.SaveCallLogAsync(dto);
            return HandleResult(result);
        }

        

        [HttpGet("GetCallHistory")]
        public async Task<IActionResult> GetCallHistory()
        {
            var result = await _callService.GetCallHistoryAsync(UserId);
            return HandleResult(result);
        }

    

        [HttpGet("GetCallHistoryWithUser/{userId}")]
        public async Task<IActionResult> GetCallHistoryWithUser(string userId)
        {
            var result = await _callService.GetCallHistoryWithUserAsync(UserId, userId);
            return HandleResult(result);
        }



        [HttpDelete("DeleteCallLog/{callId}")]
        public async Task<IActionResult> DeleteCallLog(int callId)
        {
            var result = await _callService.DeleteCallLogAsync(UserId, callId);
            return HandleResult(result);
        }
    }
}
