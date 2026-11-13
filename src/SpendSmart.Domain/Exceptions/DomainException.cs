namespace SpendSmart.Domain.Exceptions;

/// <summary>Thrown when an operation would put an entity into an invalid state.</summary>
public class DomainException(string message) : Exception(message);
