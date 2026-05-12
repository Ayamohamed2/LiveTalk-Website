using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    public class OneToOneChatController : BaseController
    {
        private readonly IChatService _chatService;

        public OneToOneChatController(IChatService chatService)
        {
            _chatService = chatService;
        }


        [HttpPost("SendMessage")]
        [EnableRateLimiting("message")]
        public async Task<IActionResult> SendMessage([FromForm] MessageDTO dto)
        {
            var result = await _chatService.SendMessageAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }



        [HttpPost("ReplyToMessage")]
        [EnableRateLimiting("message")]
        public async Task<IActionResult> ReplyToMessage([FromForm] ReplyToMessageDTOFor_OneToOne dto)
        {
            var result = await _chatService.ReplyToMessageAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }

        

        [HttpGet("AllUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _chatService.GetUsersAsync(UserId, BaseUrl);
            return HandleResult(result);
        }



        [HttpGet("messages/{userId}")]
        public async Task<IActionResult> GetMessagesWithUser(string userId)
        {
            var result = await _chatService.GetMessagesWithUserAsync(UserId, userId, BaseUrl);
            return HandleResult(result);
        }



        [HttpPost("MarkAsDelivered/{messageId}")]
        public async Task<IActionResult> MarkAsDelivered(int messageId)
        {
            var result = await _chatService.MarkAsDeliveredAsync(UserId, messageId);
            return HandleResult(result);
        }



        [HttpPost("MarkAsRead/{senderId}")]
        public async Task<IActionResult> MarkAsRead(string senderId)
        {
            var result = await _chatService.MarkAsReadAsync(UserId, senderId);
            return HandleResult(result);
        }



        [HttpPost("MarkSingleAsRead/{messageId}")]
        public async Task<IActionResult> MarkSingleAsRead(int messageId)
        {
            var result = await _chatService.MarkSingleAsReadAsync(UserId, messageId);
            return HandleResult(result);
        }

 

        [HttpGet("UnreadCount/{senderId}")]
        public async Task<IActionResult> GetUnreadCount(string senderId)
        {
            var result = await _chatService.GetUnreadCountAsync(UserId, senderId);
            return HandleResult(result);
        }



        [HttpDelete("DeleteMessageForMe/{messageId}")]
        public async Task<IActionResult> DeleteMessageForMe(int messageId)
        {
            var result = await _chatService.DeleteMessageForMeAsync(UserId, messageId);
            return HandleResult(result);
        }

       
        [HttpDelete("DeleteMessageForEveryone/{messageId}")]
        public async Task<IActionResult> DeleteMessageForEveryone(int messageId)
        {
            var result = await _chatService.DeleteMessageForEveryoneAsync(UserId, messageId);
            return HandleResult(result);
        }

      

        [HttpDelete("ClearChat/{userId}")]
        public async Task<IActionResult> ClearChat(string userId)
        {
            var result = await _chatService.ClearChatAsync(UserId, userId);
            return HandleResult(result);
        }

     

        [HttpPost("BlockUser/{userId}")]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var result = await _chatService.BlockUserAsync(UserId, userId);
            return HandleResult(result);
        }



        [HttpPost("UnblockUser/{userId}")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var result = await _chatService.UnblockUserAsync(UserId, userId);
            return HandleResult(result);
        }

       

        [HttpGet("BlockedUsers")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var result = await _chatService.GetBlockedUsersAsync(UserId, BaseUrl);
            return HandleResult(result);
        }

      

        [HttpGet("MyChatList")]
        public async Task<IActionResult> GetMyChatList()
        {
            var result = await _chatService.GetMyChatListAsync(UserId, BaseUrl);
            return HandleResult(result);
        }
    }
}
