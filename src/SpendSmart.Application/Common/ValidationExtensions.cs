using FluentValidation;
using FluentValidation.Results;

namespace SpendSmart.Application.Common;

internal static class ValidationExtensions
{
    public static ValidationException ToValidationException(this string propertyName, string message) =>
        new([new ValidationFailure(propertyName, message)]);
}
