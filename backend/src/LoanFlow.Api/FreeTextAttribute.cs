using System.ComponentModel.DataAnnotations;

namespace LoanFlow.Api;

// Free-text field: at most 100 characters, no NUL (Postgres cannot store it, AR-48.1).
public sealed class FreeTextAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text)
            return ValidationResult.Success;
        string[] members = validationContext.MemberName is { } name ? [name] : [];
        if (text.Length > 100)
            return new ValidationResult("Must be 100 characters or fewer.", members);
        if (text.Contains('\0'))
            return new ValidationResult("Contains an invalid character.", members);
        return ValidationResult.Success;
    }
}
