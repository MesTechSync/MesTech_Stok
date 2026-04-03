using System.Security.Cryptography;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Integration.Auth;

/// <summary>
/// TOTP (RFC 6238) implementation — Google Authenticator compatible.
/// HMAC-SHA1, 6-digit code, 30-second time step, +-1 window tolerance.
/// OWASP ASVS V2.8 compliant.
/// </summary>
public sealed class TotpService : ITotpService
{
    private const int SecretLength = 20; // 160-bit
    private const int TimeStep = 30;
    private const int CodeDigits = 6;
    private const int WindowTolerance = 1; // +-1 period

    // Base32 alphabet (RFC 4648)
    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public string GenerateSecret()
    {
        var secretBytes = RandomNumberGenerator.GetBytes(SecretLength);
        return ToBase32(secretBytes);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:URI return type",
        Justification = "ITotpService interface returns string — otpauth:// URI as string is standard")]
    public string GenerateQrCodeUri(string secret, string userEmail, string issuer = "MesTech")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(userEmail);

        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={CodeDigits}&period={TimeStep}";
    }

    public bool VerifyCode(string secret, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits)
            return false;

        var secretBytes = FromBase32(secret);
        var currentTimeStep = GetCurrentTimeStep();

        // Check current period and +-1 tolerance window
        for (var i = -WindowTolerance; i <= WindowTolerance; i++)
        {
            var expectedCode = ComputeTotp(secretBytes, currentTimeStep + i);
            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(expectedCode),
                    System.Text.Encoding.UTF8.GetBytes(code)))
                return true;
        }

        return false;
    }

    private static long GetCurrentTimeStep()
        => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TimeStep;

    #pragma warning disable CA5350 // HMACSHA1 required by RFC 6238 TOTP — Google Authenticator compatibility
    private static string ComputeTotp(byte[] secret, long timeStep)
    {
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timeBytes);

        // Dynamic truncation (RFC 4226 §5.4)
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binaryCode % (int)Math.Pow(10, CodeDigits);
        return otp.ToString().PadLeft(CodeDigits, '0');
    }
    #pragma warning restore CA5350

    private static string ToBase32(byte[] data)
    {
        var result = new char[(data.Length * 8 + 4) / 5];
        var buffer = 0;
        var bitsLeft = 0;
        var index = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                result[index++] = Base32Chars[(buffer >> (bitsLeft - 5)) & 0x1F];
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
            result[index] = Base32Chars[(buffer << (5 - bitsLeft)) & 0x1F];

        return new string(result);
    }

    private static byte[] FromBase32(string base32)
    {
        var cleanInput = base32.TrimEnd('=').ToUpperInvariant();
        var output = new byte[cleanInput.Length * 5 / 8];
        var buffer = 0;
        var bitsLeft = 0;
        var index = 0;

        foreach (var c in cleanInput)
        {
            var value = c switch
            {
                >= 'A' and <= 'Z' => c - 'A',
                >= '2' and <= '7' => c - '2' + 26,
                _ => throw new FormatException($"Invalid Base32 character: {c}")
            };

            buffer = (buffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output[index++] = (byte)(buffer >> (bitsLeft - 8));
                bitsLeft -= 8;
            }
        }

        return output;
    }
}
