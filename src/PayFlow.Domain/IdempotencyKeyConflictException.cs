namespace PayFlow.Domain.Exceptions;

public sealed class IdempotencyKeyConflictException() : DomainException("The Idempotency-Key was already used with a different transfer request.");
