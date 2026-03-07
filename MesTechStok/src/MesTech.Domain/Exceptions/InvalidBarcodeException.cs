namespace MesTech.Domain.Exceptions;

public class InvalidBarcodeException : DomainException
{
    public string Barcode { get; }

    public InvalidBarcodeException(string barcode)
        : base($"Invalid barcode: '{barcode}'")
    {
        Barcode = barcode;
    }
}
