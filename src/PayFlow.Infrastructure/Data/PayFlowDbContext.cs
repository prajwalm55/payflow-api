using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Data;

public sealed class PayFlowDbContext(DbContextOptions<PayFlowDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.OwnerName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Currency).HasConversion<string>().HasMaxLength(3).IsRequired();
            entity.Property(a => a.Balance).HasPrecision(18, 2);
            entity.Property(a => a.RowVersion).IsRowVersion().HasDefaultValue(new byte[8]);
        });
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Timestamp).HasConversion(value => value.UtcDateTime, value => new DateTimeOffset(value, TimeSpan.Zero));
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(100);
            entity.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            entity.HasOne(t => t.Account).WithMany(a => a.Transactions).HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.CounterpartyAccount).WithMany().HasForeignKey(t => t.CounterpartyAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
