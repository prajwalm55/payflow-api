namespace PayFlow.Domain.Exceptions;

public sealed class ConcurrencyConflictException() : DomainException("The account changed while this request was being processed. Please retry.");
