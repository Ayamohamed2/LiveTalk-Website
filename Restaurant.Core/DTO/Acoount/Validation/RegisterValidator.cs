using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.DTO.Acoount.Validation
{
    public class RegisterValidator : AbstractValidator<RegisterDTO>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters")
                .MaximumLength(50);

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required");
                

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Confirm password is required")
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");
        }
    }
}
