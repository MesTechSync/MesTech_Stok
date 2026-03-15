namespace MesTech.Domain.Exceptions;

/// <summary>
/// Validation hatasi — input/format gecersizligi.
/// Dalga 9+ standart exception.
/// </summary>
public class DomainValidationException : DomainException
{
    public string? PropertyName { get; }

    public DomainValidationException(string message)
        : base(message) { }

    public DomainValidationException(string propertyName, string message)
        : base(message)
    {
        PropertyName = propertyName;
    }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
