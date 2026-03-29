using JobFlow.Business.Models;
using JobFlow.Business.Models.DTOs;

namespace JobFlow.API.Validators;

public sealed class InviteUserDtoValidator : SafeRequestValidator<InviteUserDto>
{
    public InviteUserDtoValidator() : base("Email") { }
}

public sealed class LoginDtoValidator : SafeRequestValidator<LoginDto>
{
    public LoginDtoValidator() : base("Email", "Password") { }
}

public sealed class RegisterDtoValidator : SafeRequestValidator<RegisterDto>
{
    public RegisterDtoValidator() : base("Email", "Password") { }
}

public sealed class OrganizationRegisterDtoValidator : SafeRequestValidator<OrganizationRegisterDto>
{
    public OrganizationRegisterDtoValidator() : base("OrganizationName", "Email", "Password") { }
}

public sealed class EmployeeInviteDtoValidator : SafeRequestValidator<EmployeeInviteDto>
{
    public EmployeeInviteDtoValidator() : base("Email") { }
}

public sealed class ContactFormRequestValidator : SafeRequestValidator<ContactFormRequest>
{
    public ContactFormRequestValidator() : base("Email", "Message") { }
}

public sealed class NewsletterSubscriptionRequestValidator : SafeRequestValidator<NewsletterSubscriptionRequest>
{
    public NewsletterSubscriptionRequestValidator() : base("Email") { }
}
