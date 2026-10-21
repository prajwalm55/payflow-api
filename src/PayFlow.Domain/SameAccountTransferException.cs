using System;

namespace PayFlow.Domain.Exceptions
{
    public class SameAccountTransferException : DomainException
    {
        public SameAccountTransferException(Guid accountId)
            : base($"Cannot transfer to the same account '{accountId}'."){}
    }
}
