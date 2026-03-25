using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Tedarikçi feed dosyalarının karakter kodlamasını tespit eder.
/// Sırasıyla: BOM kontrolü → UTF-8 geçerlilik → Türkçe heuristik → varsayılan UTF-8.
/// ENT-DROP-SENTEZ-001 Sprint A — DEV 3
/// </summary>
public sealed class EncodingDetectorService : IEncodingDetectorService
{
    // ISO-8859-9 (Latin-5) — Türkçe karakterler için en yaygın Windows dışı kodlama
    private static readonly Encoding Iso88599 = GetIso88599();

    // Windows-1254 (CP1254) — Windows tabanlı Türkçe tedarikçi sistemlerinde yaygın
    private static readonly Encoding Windows1254 = GetWindows1254();

    // Türkçe özel karakterlerin ISO-8859-9 karşılıkları (byte değerleri)
    // ş=0xFE, Ş=0xDE, ğ=0xF0, Ğ=0xD0, ı=0xFD, İ=0xDD, ç=0xE7, Ç=0xC7, ö=0xF6, Ö=0xD6, ü=0xFC, Ü=0xDC
    private static readonly byte[] TurkishIso88599Bytes =
        [0xFE, 0xDE, 0xF0, 0xD0, 0xFD, 0xDD, 0xE7, 0xC7, 0xF6, 0xD6, 0xFC, 0xDC];

    // Windows-1254 Türkçe özel karakterler — ISO-8859-9 ile büyük ölçüde örtüşür
    // ş=0xFE, Ş=0xDE, ğ=0xF0, Ğ=0xD0, ı=0xFD, İ=0xDD, ç=0xE7, Ç=0xC7, ö=0xF6, Ö=0xD6, ü=0xFC, Ü=0xDC
    // (CP1254 bu aralıkta ISO-8859-9 ile aynıdır; 0x80-0x9F arası farklıdır)
    private static readonly byte[] TurkishCp1254Bytes =
        [0xFE, 0xDE, 0xF0, 0xD0, 0xFD, 0xDD, 0xE7, 0xC7, 0xF6, 0xD6, 0xFC, 0xDC,
         0x9E, 0x8E]; // ş=0x9E, Ş=0x8E sadece CP1254'te (0x80-0x9F bloğu)

    /// <inheritdoc />
    public Encoding Detect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
            return Encoding.UTF8;

        // 1. BOM kontrolü
        if (HasUtf8Bom(data))
            return Encoding.UTF8;

        if (HasUtf16LeBom(data))
            return Encoding.Unicode;

        if (HasUtf16BeBom(data))
            return Encoding.BigEndianUnicode;

        // 2. UTF-8 geçerlilik testi (BOM olmayan UTF-8 dosyalar için)
        if (IsValidUtf8(data))
            return Encoding.UTF8;

        // 3. Türkçe heuristik: CP1254'e özgü 0x80-0x9F byte'ları var mı?
        if (ContainsCp1254SpecificBytes(data))
            return Windows1254;

        // 4. ISO-8859-9 Türkçe karakter kalıpları
        if (ContainsTurkishBytes(data, TurkishIso88599Bytes))
            return Iso88599;

        // 5. Varsayılan: UTF-8
        return Encoding.UTF8;
    }

    // ─── BOM kontrol yardımcıları ──────────────────────────────────────────────

    private static bool HasUtf8Bom(byte[] data) =>
        data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF;

    private static bool HasUtf16LeBom(byte[] data) =>
        data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE;

    private static bool HasUtf16BeBom(byte[] data) =>
        data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF;

    // ─── UTF-8 geçerlilik testi ────────────────────────────────────────────────

    /// <summary>
    /// Veriyi UTF-8 olarak decode etmeye çalışır; hata yoksa UTF-8'dir.
    /// DecoderFallback.ExceptionFallback ile strict mod.
    /// </summary>
    private static bool IsValidUtf8(byte[] data)
    {
        try
        {
            var strictUtf8 = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true);
            strictUtf8.GetCharCount(data);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    // ─── Türkçe heuristik testler ──────────────────────────────────────────────

    /// <summary>
    /// CP1254'e özgü 0x80-0x9F aralığındaki Türkçe özel byte'ları arar.
    /// Bu byte'lar ISO-8859-9'da tanımsızdır; CP1254'te ş (0x9E), Ş (0x8E) vb.
    /// </summary>
    private static bool ContainsCp1254SpecificBytes(byte[] data)
    {
        // CP1254'e özgü Türkçe byte'lar (ISO-8859-9'da 0x80-0x9F tanımsız ya da farklı anlam)
        ReadOnlySpan<byte> cp1254Specific = [0x8E, 0x9E]; // Ş ve ş

        foreach (var b in data)
        {
            foreach (var specific in cp1254Specific)
            {
                if (b == specific)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verilen Türkçe byte kalıplarından herhangi birinin varlığını kontrol eder.
    /// </summary>
    private static bool ContainsTurkishBytes(byte[] data, byte[] turkishBytes)
    {
        foreach (var b in data)
        {
            foreach (var t in turkishBytes)
            {
                if (b == t)
                    return true;
            }
        }

        return false;
    }

    // ─── Encoding fabrikası ────────────────────────────────────────────────────

    /// <summary>
    /// .NET'te ISO-8859-9 code page 28599'dur.
    /// System.Text.Encoding.CodePages paketi (NuGet) gerektirir veya
    /// EncodingProvider kayıt ile çalışır. Fallback: Windows-1254.
    /// </summary>
    private static Encoding GetIso88599()
    {
        try
        {
            return Encoding.GetEncoding(28599); // ISO-8859-9
        }
        catch (ArgumentException)
        {
            // CodePages paketi kayıt edilmemişse Windows-1254 ile dön (çok yakın)
            return GetWindows1254Fallback();
        }
        catch (NotSupportedException)
        {
            return GetWindows1254Fallback();
        }
    }

    private static Encoding GetWindows1254()
    {
        try
        {
            return Encoding.GetEncoding(1254); // Windows-1254
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8; // Son çare
        }
        catch (NotSupportedException)
        {
            return Encoding.UTF8;
        }
    }

    private static Encoding GetWindows1254Fallback()
    {
        try
        {
            return Encoding.GetEncoding(1254);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }
}
