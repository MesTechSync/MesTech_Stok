namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Para değeri + döviz birimi kapsülleme.
/// Immutable value object.
/// </summary>
public record Money(decimal Amount, string Currency = "TRY")
{
    public static Money TRY(decimal amount) => new(amount, "TRY");
    public static Money USD(decimal amount) => new(amount, "USD");
    public static Money EUR(decimal amount) => new(amount, "EUR");
    public static Money Zero(string currency = "TRY") => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {Currency} and {other.Currency}");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public override string ToString() => $"{Amount:N2} {Currency}";
}
