using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.DTO.Email.Validation
{
    public class EmailValidator : AbstractValidator<EmailDTO>
    {
        public EmailValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format")
                .MaximumLength(100);
        }
    }
}
