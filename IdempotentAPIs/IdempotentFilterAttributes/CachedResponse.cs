using System;
using System.Collections.Generic;
using System.Text;

namespace IdempotentFilterAttributes
{
    public class CachedResponse
    {
        public int StatusCode { get; set; }
        public object? Value { get; set; }
    }
}
