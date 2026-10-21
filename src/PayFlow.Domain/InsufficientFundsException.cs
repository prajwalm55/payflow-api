using System;

namespace PayFlow.Domain.Exceptions
{
    public class InsufficientFundsException : DomainException
    {
        public InsufficientFundsException(decimal balance, decimal amount)
            : base($"Balance '{balance}' is insufficient for withdrawal of '{amount}'."){}
    }
}
