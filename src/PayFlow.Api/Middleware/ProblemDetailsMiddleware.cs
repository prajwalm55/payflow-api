using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Exceptions;

namespace PayFlow.Api.Middleware;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request failed for {Path}", context.Request.Path);
            var (status, title) = exception switch
            {
                AccountNotFoundException => (404, "Account not found"),
                InvalidAmountException or SameAccountTransferException or CurrencyMismatchException or InsufficientFundsException or IdempotencyKeyConflictException or DomainException => (400, "Business rule violated"),
                ConcurrencyConflictException or DbUpdateConcurrencyException => (409, "Concurrency conflict"),
                _ => (500, "Unexpected error")
            };
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title, Detail = status == 500 ? "An unexpected error occurred." : exception.Message, Instance = context.Request.Path });
        }
    }
}
