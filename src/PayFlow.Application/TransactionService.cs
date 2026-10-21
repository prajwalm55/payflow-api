using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Exceptions;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Application.Services;

public interface ITransactionService
{
    Task<TransactionDto> DepositAsync(Guid accountId, decimal amount);
    Task<TransactionDto> WithdrawAsync(Guid accountId, decimal amount);
    Task<TransactionDto> TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount, string idempotencyKey);
    Task<PagedResult<TransactionDto>> GetHistoryAsync(Guid accountId, int page, int pageSize);
}

public sealed record TransactionDto(Guid Id, Guid AccountId, Guid? CounterpartyAccountId, decimal Amount, TransactionType Type, DateTimeOffset Timestamp);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed class TransactionService(PayFlowDbContext db, ILogger<TransactionService> logger) : ITransactionService
{
    public Task<TransactionDto> DepositAsync(Guid accountId, decimal amount) => MoveAsync(accountId, amount, TransactionType.Deposit);

    public Task<TransactionDto> WithdrawAsync(Guid accountId, decimal amount) => MoveAsync(accountId, amount, TransactionType.Withdrawal);

    private async Task<TransactionDto> MoveAsync(Guid accountId, decimal amount, TransactionType type)
    {
        ValidateAmount(amount);
        var account = await FindAsync(accountId);
        if (type == TransactionType.Withdrawal && account.Balance < amount) throw new InsufficientFundsException(account.Balance, amount);
        account.Balance += type == TransactionType.Deposit ? amount : -amount;
        var transaction = new Transaction { AccountId = accountId, Amount = amount, Type = type };
        db.Transactions.Add(transaction);
        await SaveWithConcurrencyHandlingAsync();
        logger.LogInformation("Money movement {Type}: account {AccountId}, amount {Amount}, transaction {TransactionId}", type, accountId, amount, transaction.Id);
        return Map(transaction);
    }

    public async Task<TransactionDto> TransferAsync(Guid fromId, Guid toId, decimal amount, string idempotencyKey)
    {
        ValidateAmount(amount);
        if (fromId == toId) throw new SameAccountTransferException(fromId);
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new DomainException("Idempotency-Key header is required for transfers.");
        if (idempotencyKey.Length > 100) throw new DomainException("Idempotency-Key must be 100 characters or fewer.");

        var previous = await db.Transactions.AsNoTracking().SingleOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
        if (previous is not null)
        {
            if (previous.AccountId != fromId || previous.CounterpartyAccountId != toId || previous.Amount != amount) throw new IdempotencyKeyConflictException();
            return Map(previous);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var from = await FindAsync(fromId);
            var to = await FindAsync(toId);
            if (from.Currency != to.Currency) throw new CurrencyMismatchException(from.Currency, to.Currency);
            if (from.Balance < amount) throw new InsufficientFundsException(from.Balance, amount);
            from.Balance -= amount;
            to.Balance += amount;
            var transferOut = new Transaction { AccountId = fromId, CounterpartyAccountId = toId, Amount = amount, Type = TransactionType.Transfer, IdempotencyKey = idempotencyKey };
            var transferIn = new Transaction { AccountId = toId, CounterpartyAccountId = fromId, Amount = amount, Type = TransactionType.Transfer };
            db.Transactions.AddRange(transferOut, transferIn);
            await SaveWithConcurrencyHandlingAsync();
            await transaction.CommitAsync();
            logger.LogInformation("Transfer completed: from {FromAccountId} to {ToAccountId}, amount {Amount}, transaction {TransactionId}", fromId, toId, amount, transferOut.Id);
            return Map(transferOut);
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<PagedResult<TransactionDto>> GetHistoryAsync(Guid accountId, int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new DomainException("Page must be at least 1 and pageSize must be between 1 and 100.");
        _ = await FindAsync(accountId);
        var query = db.Transactions.AsNoTracking().Where(t => t.AccountId == accountId).OrderByDescending(t => t.Timestamp);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(t => new TransactionDto(t.Id, t.AccountId, t.CounterpartyAccountId, t.Amount, t.Type, t.Timestamp)).ToListAsync();
        return new PagedResult<TransactionDto>(items, page, pageSize, total);
    }

    private async Task<Account> FindAsync(Guid id) => await db.Accounts.SingleOrDefaultAsync(a => a.Id == id) ?? throw new AccountNotFoundException(id);
    private async Task SaveWithConcurrencyHandlingAsync()
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(); }
    }
    private static void ValidateAmount(decimal amount) { if (amount <= 0) throw new InvalidAmountException(amount); }
    private static TransactionDto Map(Transaction t) => new(t.Id, t.AccountId, t.CounterpartyAccountId, t.Amount, t.Type, t.Timestamp);
}
