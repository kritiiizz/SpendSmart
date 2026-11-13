using SpendSmart.Domain.Exceptions;

namespace SpendSmart.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{name} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    public static decimal Positive(decimal value, string name)
    {
        if (value <= 0)
        {
            throw new DomainException($"{name} must be greater than zero.");
        }

        return value;
    }
}
