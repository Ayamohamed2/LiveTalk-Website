using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.DTO.Acoount.Validation
{
    public class LoginValidator : AbstractValidator<LoginDTO>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required");
               
        }
    }
}
