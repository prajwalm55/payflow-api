using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Application.Tests
{
    public class AccountServiceTests
    {
        private readonly IAccountService _service;
        private readonly PayFlowDbContext _dbContext;

        public AccountServiceTests()
        {
            var services = new ServiceCollection();
            services.AddDbContext<PayFlowDbContext>(opt => opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddScoped<IAccountService, AccountService>();
            var provider = services.BuildServiceProvider();
            _dbContext = provider.GetRequiredService<PayFlowDbContext>();
            _service = provider.GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task CreateAccountAsync_ValidData_ShouldReturnDtoWithBalanceZero()
        {
            var dto = await _service.CreateAccountAsync("Bob", Currency.EUR);
            dto.Should().NotBeNull();
            dto.Balance.Should().Be(0);
            dto.Currency.Should().Be(Currency.EUR);
        }

        [Fact]
        public async Task GetBalanceAsync_ExistingAccount_ShouldReturnCorrectBalance()
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync();
            if (account == null)
            {
                var created = await _service.CreateAccountAsync("Test", Currency.USD);
                account = new PayFlow.Domain.Entities.Account { Id = created.Id, Balance = created.Balance };
            }
            var balance = await _service.GetBalanceAsync(account.Id);
            balance.Should().Be(account.Balance);
        }
    }
}
