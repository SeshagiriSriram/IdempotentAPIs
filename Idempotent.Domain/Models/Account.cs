namespace Idempotent.Domain.Models
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
