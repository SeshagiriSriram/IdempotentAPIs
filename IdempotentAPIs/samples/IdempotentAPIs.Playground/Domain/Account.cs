using System;
using System.Collections.Generic;

namespace IdempotentAPIs.Playground.Domain
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PersonId { get; set; }
        public Guid AccountTypeId { get; set; }
        public decimal Balance { get; set; }
        public Person? Person { get; set; }
        public AccountType? AccountType { get; set; }
    }
}
