using System;

namespace PayFlow.Domain.Exceptions
{
    public class InvalidAmountException : DomainException
    {
        public InvalidAmountException(decimal amount)
            : base($"Amount '{amount}' is not positive."){}
    }
}
