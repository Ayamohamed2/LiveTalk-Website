using Microsoft.Extensions.Logging;
using NEEFRA.Core;
using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Realtima_Chat_project.Models;
using Restaurant.Core.DTO.Chat;
using Restaurant.Core.Entity.Chat;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace NEEFRA.Core.Services
{
    public class CallService : ICallService
    {
        private readonly IUnitOfWork _unit;
        private readonly ILogger<CallService> _logger;

        public CallService(IUnitOfWork unit, ILogger<CallService> logger)
        {
            _unit = unit;
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        // Save Call Log
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> SaveCallLogAsync(CallDTO dto)
        {
            _logger.LogInformation("SaveCallLog: callerId={CallerId} → receiverId={ReceiverId}", dto.CallerId, dto.ReceiverId);

            var call = new Call
            {
                CallerId = dto.CallerId,
                ReceiverId = dto.ReceiverId,
                CallType = (CallType)dto.CallType,
                CallStatus = (CallStatus)dto.CallStatus,
                StartedAt = dto.StartedAt,
                EndedAt = dto.EndedAt,
                Duration = dto.Duration
            };

            _unit.Call.Create(call);
            _unit.save();

            _logger.LogInformation("SaveCallLog success: callId={CallId}", call.Id);
            return new() { IsSuccess = true, Data = (object)new { message = "Call log saved", id = call.Id } };
        }

        // ─────────────────────────────────────────────
        // Get Call History
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<IEnumerable<object>>> GetCallHistoryAsync(string userId)
        {
            _logger.LogInformation("GetCallHistory: userId={UserId}", userId);

            var callLogs = _unit.Call.GetALL(c => c.CallerId == userId || c.ReceiverId == userId)
                .OrderByDescending(c => c.StartedAt)
                .Take(50)
                .Select(c => (object)new
                {
                    c.Id,
                    c.CallerId,
                    c.ReceiverId,
                    callerName = _unit.User.GetByFilter(u => u.Id == c.CallerId)?.Name,
                    receiverName = _unit.User.GetByFilter(u => u.Id == c.ReceiverId)?.Name,
                    c.CallType,
                    c.CallStatus,
                    c.StartedAt,
                    c.EndedAt,
                    c.Duration,
                    isIncoming = c.ReceiverId == userId,
                    isOutgoing = c.CallerId == userId
                })
                .ToList();

            _logger.LogInformation("GetCallHistory success: {Count} logs for userId={UserId}", callLogs.Count, userId);
            return new() { IsSuccess = true, Data = callLogs };
        }

        // ─────────────────────────────────────────────
        // Get Call History With User
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<IEnumerable<object>>> GetCallHistoryWithUserAsync(string currentUserId, string otherUserId)
        {
            _logger.LogInformation("GetCallHistoryWithUser: currentUserId={CurrentUserId}, otherUserId={OtherUserId}", currentUserId, otherUserId);

            var callLogs = _unit.Call.GetALL(c =>
                (c.CallerId == currentUserId && c.ReceiverId == otherUserId) ||
                (c.CallerId == otherUserId && c.ReceiverId == currentUserId))
                .OrderByDescending(c => c.StartedAt)
                .Select(c => (object)new
                {
                    c.Id,
                    c.CallerId,
                    c.ReceiverId,
                    c.CallType,
                    c.CallStatus,
                    c.StartedAt,
                    c.EndedAt,
                    c.Duration,
                    isIncoming = c.ReceiverId == currentUserId
                })
                .ToList();

            _logger.LogInformation("GetCallHistoryWithUser success: {Count} logs", callLogs.Count);
            return new() { IsSuccess = true, Data = callLogs };
        }

        // ─────────────────────────────────────────────
        // Delete Call Log
        // ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> DeleteCallLogAsync(string userId, int callId)
        {
            _logger.LogInformation("DeleteCallLog: userId={UserId}, callId={CallId}", userId, callId);

            var callLog = _unit.Call.GetByFilter(c => c.Id == callId);
            if (callLog == null)
            {
                _logger.LogWarning("DeleteCallLog: call not found, callId={CallId}", callId);
                return new() { IsSuccess = false, Message = "Call log not found", ErrorType = "NotFound" };
            }

            if (callLog.CallerId != userId && callLog.ReceiverId != userId)
            {
                _logger.LogWarning("DeleteCallLog: forbidden, userId={UserId} is not caller or receiver", userId);
                return new() { IsSuccess = false, Message = "You are not authorized to delete this call log", ErrorType = "Forbidden" };
            }

            _unit.Call.Delete(callLog);
            _unit.save();

            _logger.LogInformation("DeleteCallLog success: callId={CallId}", callId);
            return new() { IsSuccess = true, Data = (object)new { message = "Call log deleted" } };
        }
    }
}
