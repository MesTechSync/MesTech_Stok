namespace MesTech.Application.Interfaces;

/// <summary>
/// Validates UBL-TR 1.2.1 e-invoice XML against GİB mandatory field requirements.
/// </summary>
public interface IUblTrXmlValidator
{
    /// <summary>
    /// Validates the given UBL-TR XML bytes and returns a list of validation errors.
    /// Empty list = valid.
    /// </summary>
    Task<IReadOnlyList<string>> ValidateAsync(byte[] xmlBytes, CancellationToken ct = default);
}
