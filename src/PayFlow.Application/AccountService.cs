using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Exceptions;
using PayFlow.Infrastructure.Data;

namespace PayFlow.Application.Services;

public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(string ownerName, Currency currency);
    Task<AccountDto> GetByIdAsync(Guid id);
    Task<decimal> GetBalanceAsync(Guid id);
}

public sealed record AccountDto(Guid Id, string OwnerName, Currency Currency, decimal Balance);

public sealed class AccountService(PayFlowDbContext db) : IAccountService
{
    public async Task<AccountDto> CreateAccountAsync(string ownerName, Currency currency)
    {
        if (string.IsNullOrWhiteSpace(ownerName)) throw new DomainException("Owner name is required.");
        var account = new Account { OwnerName = ownerName.Trim(), Currency = currency };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return Map(account);
    }

    public async Task<AccountDto> GetByIdAsync(Guid id) => Map(await FindAsync(id));

    public async Task<decimal> GetBalanceAsync(Guid id) => (await FindAsync(id)).Balance;

    private async Task<Account> FindAsync(Guid id) => await db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id)
        ?? throw new AccountNotFoundException(id);

    private static AccountDto Map(Account a) => new(a.Id, a.OwnerName, a.Currency, a.Balance);
}
