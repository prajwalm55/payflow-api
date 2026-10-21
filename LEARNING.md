# PayFlow learning guide

## How to explain the project

- `src/PayFlow.Api` is the HTTP boundary. Controllers turn JSON into service calls. `ProblemDetailsMiddleware` creates consistent errors. `Program.cs` wires dependencies, Swagger, health checks, SQL Server, and migrations.
- `src/PayFlow.Application` contains use cases. `AccountService` creates and reads accounts. `TransactionService` validates amounts, changes balances, writes history, logs movements, and coordinates transfers. DTOs keep database entities out of the public API.
- `src/PayFlow.Domain` contains `Account`, `Transaction`, currency and transaction enums, and named business exceptions.
- `src/PayFlow.Infrastructure` contains `PayFlowDbContext`, EF Core mappings, the SQL Server provider, and `Migrations/InitialCreate.cs`. The rowversion mapping enables optimistic concurrency.
- `tests` contains fast service tests and an HTTP integration test. The integration factory uses relational SQLite in memory, so CI needs no external database.

A transfer validates the request, checks the stored idempotency key, opens one transaction, loads both accounts, checks currency and funds, updates both balances, writes two history rows, commits, and returns the transfer-out row. Any failure rolls back.

## Ten interview questions

1. **Why a database transaction?** To make debit and credit atomic.
2. **What is idempotency?** Retrying a request does not repeat its side effect.
3. **Where is the idempotency key stored?** On the transfer-out row with a unique index, so it survives restarts.
4. **What is optimistic concurrency?** EF detects when another update changed the row first using rowversion.
5. **Why two transfer history rows?** Each account can query its own statement directly.
6. **Why use DTOs?** Persistence details stay private and the HTTP contract can evolve.
7. **Why is SQL Server in Infrastructure?** Database/provider details stay outside domain and use-case code.
8. **What happens on an overdraft?** A named domain exception becomes a ProblemDetails 400 response.
9. **Why page history?** To avoid loading or returning unbounded transaction data.
10. **What comes next?** Identity, authorization, observability, an outbox, and provider-specific integration tests.
