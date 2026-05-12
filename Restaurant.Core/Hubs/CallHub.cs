using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Realtima_Chat_project.Hubs
{
    public class CallHub : Hub
    {

        private static readonly ConcurrentDictionary<string, CallInfo> ActiveCalls
    = new ConcurrentDictionary<string, CallInfo>();

        public class CallInfo
        {
            public string CallerId { get; set; }
            public string CallerName { get; set; }
            public string ReceiverId { get; set; }
            public string ReceiverName { get; set; }
            public CallType Type { get; set; }
            public DateTime StartedAt { get; set; }
            public CallStatus Status { get; set; }
        }

        public enum CallType
        {
            Voice = 0,
            Video = 1
        }

        public enum CallStatus
        {
            Ringing = 0,
            Active = 1,
            Ended = 2,
            Rejected = 3,
            Missed = 4,
            Busy = 5
        }

        public async Task InitiateCall(string receiverId, string receiverName, int callType)
        {
            var callerId = Context.UserIdentifier;
            var callerName = Context.User?.Identity?.Name ?? "Unknown";
            if (string.IsNullOrEmpty(callerId)) return;

            var recievercalls = ActiveCalls.Values.Any(c => (c.CallerId == receiverId || c.ReceiverId == receiverId) &&
               (c.Status == CallStatus.Active));

            if (recievercalls)
            {
                await Clients.Caller.SendAsync("CallBusy", receiverId);
                return;
            }
            var callId = Guid.NewGuid().ToString();
            var callInfo = new CallInfo
            {
                CallerId = callerId,
                CallerName = callerName,
                ReceiverId = receiverId,
                ReceiverName = receiverName,
                Type = (CallType)callType,
                StartedAt = DateTime.Now,
                Status = CallStatus.Ringing
            };
            ActiveCalls.TryAdd(callId, callInfo);

            await Clients.User(receiverId).SendAsync("IncomingCall", new
            {
                CallId = callId,
                CallerId = callerId,
                CallerName = callerName,
                CallType = callType,
                StartedAt = callInfo.StartedAt
            });

            await Clients.Caller.SendAsync("CallInitiated", new
            {
                CallId = callId,
                ReceiverId = receiverId,
                ReceiverName = receiverName,
                CallType = callType
            });
        }

        public async Task AcceptCall(string callId)
        {
            if (!ActiveCalls.TryGetValue(callId, out var callInfo))
            {
                await Clients.Caller.SendAsync("CallNotFound");
                return;
            }

            callInfo.Status = CallStatus.Active;
            await Clients.User(callInfo.CallerId).SendAsync("CallAccepted", new
            {
                CallId = callId,
                ReceiverId = callInfo.ReceiverId
            });

            await Clients.Caller.SendAsync("CallConnected", new
            {
                CallId = callId,
                CallerId = callInfo.CallerId
            });
        }


        public async Task RejectCall(string callId)
        {
            if (!ActiveCalls.TryRemove(callId, out var callInfo))
            {
                return;
            }

            callInfo.Status = CallStatus.Rejected;

            await Clients.User(callInfo.CallerId).SendAsync("CallRejected", new
            {
                CallId = callId,
                ReceiverId = callInfo.ReceiverId
            });

        }

        public async Task EndCall(string callId)
        {
            if (!ActiveCalls.TryRemove(callId, out var callInfo))
            {
                return;
            }
            callInfo.Status = CallStatus.Ended;


            var userId = Context.UserIdentifier;
            var Duration = (DateTime.Now - callInfo.StartedAt).TotalSeconds;

            var otheruser = userId == callInfo.CallerId ? callInfo.ReceiverId : callInfo.CallerId;

            await Clients.User(otheruser).SendAsync("CallEnded", new
            {
                CallId = callId,
                Duration = Duration,
                EndedBy = userId
            });


            await Clients.Caller.SendAsync("CallTerminated", new
            {
                CallId = callId,
                Duration = Duration
            });
        }
        public async Task SendOffer(string callId, string receiverId, object offer)
        {
            await Clients.User(receiverId).SendAsync("ReceiveOffer", new
            {
                CallId = callId,
                Offer = offer
            });

        }


        public async Task SendAnswer(string callId, string callerId, object answer)
        {
            await Clients.User(callerId).SendAsync("ReceiveAnswer", new
            {
                CallId = callId,
                Answer = answer
            });

        }

        public async Task SendIceCandidate(string callId, string targetUserId, object candidate)
        {
            await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", new
            {
                CallId = callId,
                Candidate = candidate
            });
        }


        public async Task ToggleMute(string callId, bool isMuted)
        {
            if (!ActiveCalls.TryGetValue(callId, out var callInfo))
            {
                return;
            }

            var userId = Context.UserIdentifier;
            var otherUserId = userId == callInfo.CallerId ? callInfo.ReceiverId : callInfo.CallerId;

            await Clients.User(otherUserId).SendAsync("UserMuteStatusChanged", new
            {
                CallId = callId,
                UserId = userId,
                IsMuted = isMuted
            });
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;

            var userCalls = ActiveCalls.Where(c =>
                c.Value.CallerId == userId || c.Value.ReceiverId == userId).ToList();

            foreach (var call in userCalls)
            {
                if (ActiveCalls.TryRemove(call.Key, out var callInfo))
                {
                    var otherUserId = userId == callInfo.CallerId ? callInfo.ReceiverId : callInfo.CallerId;

                    await Clients.User(otherUserId).SendAsync("CallEnded", new
                    {
                        CallId = call.Key,
                        Reason = "Connection lost"
                    });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
