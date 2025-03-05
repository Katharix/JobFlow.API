using FluentValidation;
using JobFlow.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Validators
{
    public class OrganizationValidator : AbstractValidator<Organization>
    {
        public OrganizationValidator()
        {
            RuleFor(x => x.OrganizationName)
                .NotEmpty()
                .WithMessage("Organization Name is required.");

            RuleFor(x => x.ZipCode)
                .InclusiveBetween(10000, 99999)
                .When(x => x.ZipCode.HasValue)
                .WithMessage("Zip Code must be a 5-digit number.");

            RuleFor(x => x.EmailAddress)
                .NotEmpty()
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress))
                .WithMessage("Invalid email format.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[\d\s()-]{7,15}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage("Invalid phone number format.");

            RuleFor(x => x.Address1)
                .NotEmpty()
                .WithMessage("Address1 is required.");

            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("City is required.");

            RuleFor(x => x.State)
                .NotEmpty()
                .WithMessage("State is required.");

            RuleFor(x => x.OrganizationTypeId)
                .NotEmpty()
                .WithMessage("Organization Type is required.");
        }
    }
}
