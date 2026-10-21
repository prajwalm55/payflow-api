namespace PayFlow.Domain.Entities;

public enum TransactionType { Deposit, Withdrawal, Transfer }

public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid? CounterpartyAccountId { get; set; }
    public Account? CounterpartyAccount { get; set; }
    public string? IdempotencyKey { get; set; }
}
