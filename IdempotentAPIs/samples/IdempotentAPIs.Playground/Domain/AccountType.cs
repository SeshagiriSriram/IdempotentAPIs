using System;
using System.Collections.Generic;

namespace IdempotentAPIs.Playground.Domain
{
    public class AccountType
    {
        public Guid Id { get; set; } // GUID based Account Type
        public string Name { get; set; } = string.Empty;
    }

}
