namespace MesTech.Application.Interfaces;

/// <summary>
/// Barkod gorsel uretim servisi — Code128, EAN-13 destegi.
/// SVG ve QuestPDF PDF cikti formatlari.
/// S06f — DEV 6.
/// </summary>
public interface IBarcodeGenerationService
{
    /// <summary>Code128 barkod PNG goruntusunu QuestPDF ile uretir.</summary>
    byte[] GenerateCode128Png(string data, int width = 400, int height = 80);

    /// <summary>EAN-13 barkod PNG goruntusunu QuestPDF ile uretir.</summary>
    byte[] GenerateEan13Png(string data, int width = 300, int height = 100);

    /// <summary>Code128 barkod SVG ciktisi uretir (pure string — harici kutuphane gerektirmez).</summary>
    string GenerateCode128Svg(string data);
}
