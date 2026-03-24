namespace MesTech.Infrastructure.Security;

/// <summary>
/// PII (Personally Identifiable Information) maskeleme helper'ı.
/// Log mesajlarında email, vergi numarası, telefon gibi hassas verileri maskeler.
/// KÇ-07 / D20 uyumu: PII maskeleme yoksa log'da kişisel veri açık yazar.
/// </summary>
public static class PiiLogMaskHelper
{
    /// <summary>
    /// Email adresini maskeler: us***@do***.com
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "***";

        var atIndex = email.IndexOf('@');
        if (atIndex < 1)
            return "***@***";

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];

        var maskedLocal = local.Length <= 2
            ? local[..1] + "***"
            : local[..2] + "***";

        var dotIndex = domain.LastIndexOf('.');
        var maskedDomain = dotIndex > 2
            ? domain[..2] + "***" + domain[dotIndex..]
            : domain;

        return maskedLocal + "@" + maskedDomain;
    }

    /// <summary>
    /// Vergi numarasını maskeler: 123***7890
    /// </summary>
    public static string MaskTaxNumber(string? taxNumber)
    {
        if (string.IsNullOrWhiteSpace(taxNumber))
            return "***";

        return taxNumber.Length switch
        {
            <= 3 => "***",
            <= 6 => taxNumber[..1] + new string('*', taxNumber.Length - 2) + taxNumber[^1..],
            _ => taxNumber[..3] + new string('*', taxNumber.Length - 6) + taxNumber[^3..]
        };
    }

    /// <summary>
    /// Telefon numarasını maskeler: +90***12
    /// </summary>
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "***";

        return phone.Length > 5
            ? phone[..3] + new string('*', phone.Length - 5) + phone[^2..]
            : "***";
    }
}
