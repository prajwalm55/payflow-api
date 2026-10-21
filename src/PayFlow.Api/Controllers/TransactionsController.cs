using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Services;
using System;
using System.Threading.Tasks;

namespace PayFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("deposit")]
        public async Task<ActionResult<TransactionDto>> Deposit(TransactionRequest request)
        {
            var result = await _transactionService.DepositAsync(request.AccountId, request.Amount);
            return Ok(result);
        }

        [HttpPost("withdraw")]
        public async Task<ActionResult<TransactionDto>> Withdraw(TransactionRequest request)
        {
            var result = await _transactionService.WithdrawAsync(request.AccountId, request.Amount);
            return Ok(result);
        }

        [HttpPost("transfer")]
        public async Task<ActionResult<TransactionDto>> Transfer(TransferRequest request)
        {
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            var result = await _transactionService.TransferAsync(request.FromAccountId, request.ToAccountId, request.Amount, idempotencyKey.ToString());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PagedResult<TransactionDto>>> History(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _transactionService.GetHistoryAsync(id, page, pageSize));
        }
    }

    public sealed record TransactionRequest(Guid AccountId, decimal Amount);
    public sealed record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount);
}
