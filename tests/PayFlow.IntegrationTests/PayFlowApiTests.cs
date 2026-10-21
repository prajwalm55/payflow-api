using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Data;

namespace PayFlow.IntegrationTests;

public sealed class PayFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<PayFlowDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<PayFlowDbContext>(options => options.UseSqlite(connection));
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<PayFlowDbContext>().Database.EnsureCreated();
        });
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public sealed class PayFlowApiTests : IClassFixture<PayFlowApiFactory>
{
    private readonly HttpClient client;
    public PayFlowApiTests(PayFlowApiFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task Transfer_is_idempotent_and_history_is_paged()
    {
        var from = await CreateAccount("Alice");
        var to = await CreateAccount("Bob");
        (await client.PostAsJsonAsync("/api/transactions/deposit", new { accountId = from.Id, amount = 100 })).EnsureSuccessStatusCode();
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/transactions/transfer") { Content = JsonContent.Create(new { fromAccountId = from.Id, toAccountId = to.Id, amount = 40 }) };
        firstRequest.Headers.Add("Idempotency-Key", "integration-transfer-1");
        (await client.SendAsync(firstRequest)).IsSuccessStatusCode.Should().BeTrue();
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/transactions/transfer") { Content = JsonContent.Create(new { fromAccountId = from.Id, toAccountId = to.Id, amount = 40 }) };
        secondRequest.Headers.Add("Idempotency-Key", "integration-transfer-1");
        (await client.SendAsync(secondRequest)).IsSuccessStatusCode.Should().BeTrue();
        var balance = await client.GetFromJsonAsync<BalanceResponse>($"/api/accounts/{from.Id}/balance");
        balance!.Balance.Should().Be(60);
        var history = await client.GetFromJsonAsync<PagedHistory>($"/api/transactions/{from.Id}?page=1&pageSize=1");
        history!.Items.Should().HaveCount(1); history.TotalCount.Should().Be(2);
    }

    private async Task<AccountResponse> CreateAccount(string owner)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new { ownerName = owner, currency = "USD" });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }
    private sealed record BalanceResponse(decimal Balance);
    private sealed record AccountResponse(Guid Id);
    private sealed record PagedHistory(IReadOnlyList<TransactionResponse> Items, int Page, int PageSize, int TotalCount);
    private sealed record TransactionResponse(Guid Id, Guid AccountId, Guid? CounterpartyAccountId, decimal Amount, string Type, DateTimeOffset Timestamp);
}
