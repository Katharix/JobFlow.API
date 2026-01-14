namespace JobFlow.Business.Extensions;

public static class DecimalExtensions
{
    /// <summary>
    /// Converts a decimal dollar amount to cents (smallest currency unit).
    /// Example: 10.00 → 1000
    /// </summary>
    public static long ToCents(this decimal amount)
    {
        return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
    }
    public static long? ToCentsNullable(this decimal? amount)
    {
        if (!amount.HasValue)
        {
            return null;
        }
        return (long)Math.Round(amount.Value * 100, MidpointRounding.AwayFromZero);
    }
}