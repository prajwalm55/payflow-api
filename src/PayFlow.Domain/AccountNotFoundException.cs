using System;

namespace PayFlow.Domain.Exceptions
{
    public class AccountNotFoundException : DomainException
    {
        public AccountNotFoundException(Guid accountId)
            : base($"Account '{accountId}' not found."){}
    }
}
