namespace PayFlow.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerName { get; set; } = string.Empty;
    public Currency Currency { get; set; }
    public decimal Balance { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
