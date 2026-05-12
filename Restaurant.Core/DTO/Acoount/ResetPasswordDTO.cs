using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Core.DTO.Acoount
{
    public class ResetPasswordDTO
    {

        
        public string NewPassword { get; set; }
      
        public string ConfirmPassword { get; set; }
    }
}
