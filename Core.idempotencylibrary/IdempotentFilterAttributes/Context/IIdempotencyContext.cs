namespace IdempotentFilterAttributes.Context
{
    public interface IIdempotencyContext
    {
        public Guid LedgerId { get; set; }
    }
}
