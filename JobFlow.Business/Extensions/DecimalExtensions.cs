namespace JobFlow.Business.Extensions;

public static class DecimalExtensions
{
    /// <summary>
    ///     Converts a decimal dollar amount to cents (smallest currency unit).
    ///     Example: 10.00 → 1000 (cents)
    /// </summary>
    /// <param name="amount">The amount in dollars</param>
    /// <returns>Equivalent amount in cents</returns>
    public static long ToCents(this decimal amount)
    {
        return (long)(amount * 100);
    }
}