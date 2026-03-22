namespace MesTech.Domain.Exceptions;

public sealed class DuplicateSKUException : DomainException
{
    public string SKU { get; }

    public DuplicateSKUException(string sku)
        : base($"A product with SKU '{sku}' already exists.")
    {
        SKU = sku;
    }
}
