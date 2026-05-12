using System.ComponentModel.DataAnnotations;

namespace Restaurant.Core.DTO.Email
{
    public class EmailDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
