using PayFlow.Domain.Entities;
using System;

namespace PayFlow.Domain.Exceptions
{
    public class CurrencyMismatchException : DomainException
    {
        public CurrencyMismatchException(Currency a, Currency b)
            : base($"Currency mismatch: '{a}' vs '{b}'."){}
    }
}
