namespace MesTech.Domain.Exceptions;

/// <summary>
/// Tüm domain exception'larının temel sınıfı.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}
