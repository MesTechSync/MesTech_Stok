namespace MesTech.Domain.Exceptions;

/// <summary>
/// Is kurali ihlali — gecerli input ama is mantigi reddetti.
/// Dalga 9+ standart exception.
/// </summary>
public sealed class BusinessRuleException : DomainException
{
    public string? RuleName { get; }

    public BusinessRuleException(string message)
        : base(message) { }

    public BusinessRuleException(string ruleName, string message)
        : base($"[{ruleName}] {message}")
    {
        RuleName = ruleName;
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException) { }
}
