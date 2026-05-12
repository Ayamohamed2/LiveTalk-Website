using NEEFRA.Core.DTO.Service;
using Realtima_Chat_project.DTO;
using Restaurant.Core.DTO.Chat;

namespace NEEFRA.Core
{
    public interface ICallService
    {
        Task<ServiceResult<object>> SaveCallLogAsync(CallDTO dto);
        Task<ServiceResult<IEnumerable<object>>> GetCallHistoryAsync(string userId);
        Task<ServiceResult<IEnumerable<object>>> GetCallHistoryWithUserAsync(string currentUserId, string otherUserId);
        Task<ServiceResult<object>> DeleteCallLogAsync(string userId, int callId);
    }
}
