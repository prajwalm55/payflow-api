using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Exceptions;
using PayFlow.Infrastructure.Data;

namespace PayFlow.UnitTests;

public sealed class TransactionServiceTests
{
    private static (PayFlowDbContext Db, TransactionService Service) Create()
    {
        var options = new DbContextOptionsBuilder<PayFlowDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
        var db = new PayFlowDbContext(options);
        return (db, new TransactionService(db, NullLogger<TransactionService>.Instance));
    }

    private static async Task<Account> AccountAsync(PayFlowDbContext db, string owner = "A", Currency currency = Currency.USD, decimal balance = 0)
    {
        var account = new Account { OwnerName = owner, Currency = currency, Balance = balance };
        db.Accounts.Add(account); await db.SaveChangesAsync(); return account;
    }

    [Fact] public async Task Deposit_increases_balance() { var (db, s) = Create(); var a = await AccountAsync(db); await s.DepositAsync(a.Id, 50); (await db.Accounts.FindAsync(a.Id))!.Balance.Should().Be(50); }
    [Fact] public async Task Withdraw_decreases_balance() { var (db, s) = Create(); var a = await AccountAsync(db, balance: 50); await s.WithdrawAsync(a.Id, 20); (await db.Accounts.FindAsync(a.Id))!.Balance.Should().Be(30); }
    [Fact] public async Task Overdraft_is_rejected() { var (db, s) = Create(); var a = await AccountAsync(db); Func<Task> act = () => s.WithdrawAsync(a.Id, 1); await act.Should().ThrowAsync<InsufficientFundsException>(); }
    [Fact] public async Task Invalid_amount_is_rejected() { var (_, s) = Create(); Func<Task> act = () => s.DepositAsync(Guid.NewGuid(), 0); await act.Should().ThrowAsync<InvalidAmountException>(); }
    [Fact] public async Task Transfer_moves_money() { var (db, s) = Create(); var from = await AccountAsync(db, balance: 100); var to = await AccountAsync(db); await s.TransferAsync(from.Id, to.Id, 40, "unit-1"); (await db.Accounts.FindAsync(from.Id))!.Balance.Should().Be(60); (await db.Accounts.FindAsync(to.Id))!.Balance.Should().Be(40); }
    [Fact] public async Task Same_account_transfer_is_rejected() { var (db, s) = Create(); var a = await AccountAsync(db); Func<Task> act = () => s.TransferAsync(a.Id, a.Id, 1, "unit-2"); await act.Should().ThrowAsync<SameAccountTransferException>(); }
    [Fact] public async Task Currency_mismatch_is_rejected() { var (db, s) = Create(); var from = await AccountAsync(db, currency: Currency.USD, balance: 10); var to = await AccountAsync(db, currency: Currency.EUR); Func<Task> act = () => s.TransferAsync(from.Id, to.Id, 1, "unit-3"); await act.Should().ThrowAsync<CurrencyMismatchException>(); }
}
