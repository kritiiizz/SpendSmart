namespace SpendSmart.Application.Common;

/// <summary>The resource doesn't exist, or belongs to someone else (we don't reveal which).</summary>
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

/// <summary>The request is valid but clashes with the current state, e.g. deleting a category in use.</summary>
public sealed class ConflictException(string message) : Exception(message);
