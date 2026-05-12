using System.ComponentModel.DataAnnotations;

namespace Restaurant.Core.DTO.Acoount
{
    public class LoginDTO
    {
       
        public string Email { get; set; }
        
        public string Password { get; set; }
    }
}
