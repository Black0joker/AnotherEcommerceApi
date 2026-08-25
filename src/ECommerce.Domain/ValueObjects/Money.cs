namespace ECommerce.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency = "USD")
{
    public static Money Zero(string currency = "USD") => new(0m, currency);

    public static Money FromDecimal(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        return new Money(amount, currency);
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount, left.Currency);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount, left.Currency);
    public static Money operator *(Money money, int multiplier) => new(money.Amount * multiplier, money.Currency);
}
