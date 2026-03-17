using System.Reflection;
using System.Text.RegularExpressions;
using FluentValidation;

namespace JobFlow.API.Validators;

internal static class ValidationConventions
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void ValidateObjectStrings<T>(
        T instance,
        ValidationContext<T> context,
        IReadOnlyCollection<string>? requiredStringFields = null) where T : class
    {
        if (instance is null)
        {
            context.AddFailure("Request", "Request body is required.");
            return;
        }

        var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            if (prop.PropertyType != typeof(string) || !prop.CanRead)
                continue;

            var value = prop.GetValue(instance) as string;

            if (requiredStringFields?.Contains(prop.Name, StringComparer.OrdinalIgnoreCase) == true && string.IsNullOrWhiteSpace(value))
            {
                context.AddFailure(prop.Name, $"{prop.Name} is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (value.Length > 4000)
                context.AddFailure(prop.Name, "Value exceeds maximum length.");

            if (prop.Name.Contains("Email", StringComparison.OrdinalIgnoreCase) && !EmailRegex.IsMatch(value))
                context.AddFailure(prop.Name, "Invalid email format.");

            if ((prop.Name.Contains("Url", StringComparison.OrdinalIgnoreCase) || prop.Name.Contains("Uri", StringComparison.OrdinalIgnoreCase)) &&
                !Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                context.AddFailure(prop.Name, "Invalid URL format.");
            }
        }
    }
}
