using System;
using FluentAssertions;
using Xunit;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Exceptions;

namespace PayFlow.Domain.Tests
{
    public class AccountTests
    {
        [Fact]
        public void CreateAccount_WithValidData_ShouldSetProperties()
        {
            var account = new Account
            {
                OwnerName = "Alice",
                Currency = Currency.USD,
                Balance = 100m
            };

            account.Id.Should().NotBeEmpty();
            account.OwnerName.Should().Be("Alice");
            account.Currency.Should().Be(Currency.USD);
            account.Balance.Should().Be(100m);
            account.RowVersion.Should().NotBeNull();
        }
    }
}
