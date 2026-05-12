using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NEEFRA.Core;
using Realtima_Chat_project.DTO;
using Restaurant.API.Controllers;
using Restaurant.Core.DTO.Group;

namespace Realtima_Chat_project.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class GroupController : BaseController
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

   

        [HttpPost("Create")]
        [EnableRateLimiting("group")]
        public async Task<IActionResult> CreateGroup([FromForm] CreateGroupDTO dto)
        {
            var result = await _groupService.CreateGroupAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }



        [HttpPost("Join")]
        public async Task<IActionResult> JoinGroup([FromBody] JoinGroupDTO dto)
        {
            var result = await _groupService.JoinGroupAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }



        [HttpPost("AddMembers")]
        public async Task<IActionResult> AddMembers([FromBody] AddMembersDTO dto)
        {
            var result = await _groupService.AddMembersAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }

   

        [HttpPost("RemoveMember")]
        public async Task<IActionResult> RemoveMember([FromBody] RemoveMemberDTO dto)
        {
            var result = await _groupService.RemoveMemberAsync(UserId, dto);
            return HandleResult(result);
        }

 

        [HttpPost("Leave/{groupId}")]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var result = await _groupService.LeaveGroupAsync(UserId, groupId);
            return HandleResult(result);
        }



        [HttpPut("UpdateGroup/{groupId}")]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromForm] UpdateGroupDTO dto)
        {
            var result = await _groupService.UpdateGroupAsync(UserId, groupId, dto, BaseUrl);
            return HandleResult(result);
        }


        [HttpGet("GetGroup/{groupId}")]
        public async Task<IActionResult> GetGroupById(int groupId)
        {
            var result = await _groupService.GetGroupByIdAsync(UserId, groupId, BaseUrl);
            return HandleResult(result);
        }



        [HttpGet("MyGroups")]
        public async Task<IActionResult> GetMyGroups()
        {
            var result = await _groupService.GetMyGroupsAsync(UserId, BaseUrl);
            return HandleResult(result);
        }



        [HttpGet("Members/{groupId}")]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {
            var result = await _groupService.GetGroupMembersAsync(UserId, groupId, BaseUrl);
            return HandleResult(result);
        }



        [HttpGet("AvailableUsers/{groupId}")]
        public async Task<IActionResult> GetAvailableUsers(int groupId)
        {
            var result = await _groupService.GetAvailableUsersAsync(UserId, groupId, BaseUrl);
            return HandleResult(result);
        }


        [HttpPost("SendMessage")]
        [EnableRateLimiting("message")]
        public async Task<IActionResult> SendMessage([FromForm] SendGroupMessageDTO dto)
        {
            var result = await _groupService.SendMessageAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }



        [HttpPost("ReplyToMessage")]
        [EnableRateLimiting("message")]
        public async Task<IActionResult> ReplyToMessage([FromForm] ReplyToMessageDTO dto)
        {
            var result = await _groupService.ReplyToMessageAsync(UserId, dto, BaseUrl);
            return HandleResult(result);
        }



        [HttpGet("Messages/{groupId}")]
        public async Task<IActionResult> GetGroupMessages(int groupId)
        {
            var result = await _groupService.GetGroupMessagesAsync(UserId, groupId, BaseUrl);
            return HandleResult(result);
        }


        [HttpDelete("DeleteMessageForMe/{messageId}")]
        public async Task<IActionResult> DeleteMessageForMe(int messageId)
        {
            var result = await _groupService.DeleteMessageForMeAsync(UserId, messageId);
            return HandleResult(result);
        }


        [HttpDelete("DeleteMessageForEveryone/{messageId}")]
        public async Task<IActionResult> DeleteMessageForEveryone(int messageId)
        {
            var result = await _groupService.DeleteMessageForEveryoneAsync(UserId, messageId);
            return HandleResult(result);
        }

        

        [HttpDelete("ClearChat/{groupId}")]
        public async Task<IActionResult> ClearChatForMe(int groupId)
        {
            var result = await _groupService.ClearChatAsync(UserId, groupId);
            return HandleResult(result);
        }


        [HttpPost("MarkAsRead/{messageId}")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var result = await _groupService.MarkAsReadAsync(UserId, messageId);
            return HandleResult(result);
        }

     

        [HttpGet("WhoRead/{messageId}")]
        public async Task<IActionResult> WhoReadMessage(int messageId)
        {
            var result = await _groupService.WhoReadAsync(UserId, messageId, BaseUrl);
            return HandleResult(result);
        }

    

        [HttpGet("UnreadCount/{groupId}")]
        public async Task<IActionResult> GetUnreadCount(int groupId)
        {
            var result = await _groupService.GetUnreadCountAsync(UserId, groupId);
            return HandleResult(result);
        }
    }
}
