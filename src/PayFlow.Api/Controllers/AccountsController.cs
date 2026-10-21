using Microsoft.AspNetCore.Mvc;
using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace PayFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<ActionResult<AccountDto>> Create(CreateAccountRequest request)
        {
            var result = await _accountService.CreateAccountAsync(request.OwnerName, request.Currency);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AccountDto>> GetById(Guid id)
        {
            var account = await _accountService.GetByIdAsync(id);
            return Ok(account);
        }

        [HttpGet("{id}/balance")]
        public async Task<ActionResult<object>> GetBalance(Guid id)
        {
            var balance = await _accountService.GetBalanceAsync(id);
            return Ok(new { Balance = balance });
        }
    }

    public sealed record CreateAccountRequest(string OwnerName, Currency Currency);
}
