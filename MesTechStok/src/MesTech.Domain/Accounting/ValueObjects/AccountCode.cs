using System.Text.RegularExpressions;

namespace MesTech.Domain.Accounting.ValueObjects;

/// <summary>
/// Hesap kodu value object — "100.01.001" formatinda 3 haneli gruplar.
/// Immutable.
/// </summary>
public sealed partial record AccountCode
{
    /// <summary>
    /// Format: Nokta ile ayrilmis 3 haneli gruplar (min 1 grup, max 5 grup).
    /// Ornekler: "100", "100.01", "100.01.001"
    /// </summary>
    private static readonly Regex Pattern = AccountCodeRegex();

    public string Code { get; }

    public AccountCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!Pattern.IsMatch(code))
            throw new ArgumentException(
                $"Invalid account code format: '{code}'. Expected format: '100.01.001' (dot-separated digit groups).",
                nameof(code));

        Code = code;
    }

    /// <summary>
    /// Hesap seviyesini dondurur (nokta sayisi + 1).
    /// "100" -> 1, "100.01" -> 2, "100.01.001" -> 3
    /// </summary>
    public int Level => Code.Split('.').Length;

    /// <summary>
    /// Ust hesap kodunu dondurur. En ust seviye icin null.
    /// </summary>
    public AccountCode? Parent
    {
        get
        {
            var lastDot = Code.LastIndexOf('.');
            return lastDot < 0 ? null : new AccountCode(Code[..lastDot]);
        }
    }

    public override string ToString() => Code;

    public static implicit operator string(AccountCode accountCode) => accountCode.Code;

    [GeneratedRegex(@"^\d{3}(?:\.\d{2,3})*$", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex AccountCodeRegex();
}
