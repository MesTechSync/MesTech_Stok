namespace MesTech.Application.Interfaces;

/// <summary>
/// TOTP (RFC 6238) servisi — Google Authenticator uyumlu.
/// OWASP ASVS V2.8 gereksinimi.
/// </summary>
public interface ITotpService
{
    /// <summary>Yeni 160-bit TOTP secret uretir (Base32 encoded).</summary>
    string GenerateSecret();

    /// <summary>Secret icin QR code URI uretir (otpauth:// format).</summary>
    string GenerateQrCodeUri(string secret, string userEmail, string issuer = "MesTech");

    /// <summary>Kullanicinin girdigi 6 haneli kodu dogrular (30sn pencere, +-1 tolerans).</summary>
    bool VerifyCode(string secret, string code);
}
