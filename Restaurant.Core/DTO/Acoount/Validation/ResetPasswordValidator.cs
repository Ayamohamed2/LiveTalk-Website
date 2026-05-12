using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.DTO.Acoount.Validation
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordDTO>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required");
               

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Confirm password is required")
                .Equal(x => x.NewPassword)
                .WithMessage("Passwords do not match");
        }
    }
}
