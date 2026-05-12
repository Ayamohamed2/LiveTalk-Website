namespace Restaurant.Core.DTO.Chat
{
    public class CallDTO
    {
        public string CallerId { get; set; }
        public string ReceiverId { get; set; }
        public int CallType { get; set; }
        public int CallStatus { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int Duration { get; set; }
    }
}
