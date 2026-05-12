namespace Restaurant.Core.DTO.Chat
{
    public class MessageReadInfoDTO
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string? UserImage { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
