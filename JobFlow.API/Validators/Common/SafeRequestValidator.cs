using FluentValidation;

namespace JobFlow.API.Validators;

public abstract class SafeRequestValidator<T> : AbstractValidator<T> where T : class
{
    protected SafeRequestValidator(params string[] requiredStringFields)
    {
        RuleFor(x => x)
            .NotNull()
            .Custom((instance, context) =>
            {
                ValidationConventions.ValidateObjectStrings(instance, context, requiredStringFields);
            });
    }
}
