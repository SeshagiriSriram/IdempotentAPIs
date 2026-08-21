namespace  IdempotentFilterAttributes.Context
{
    public class IdempotencyContext: IIdempotencyContext
    {
         public Guid LedgerId { get; set; }
    }
}
