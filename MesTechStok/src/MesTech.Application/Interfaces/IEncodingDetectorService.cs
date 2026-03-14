using System.Text;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Tedarikçi feed dosyalarının karakter kodlamasını tespit eder.
/// UTF-8, ISO-8859-9 (Latin-5) ve Windows-1254 (CP1254) desteklenir.
/// ENT-DROP-SENTEZ-001 Sprint A — DEV 3
/// </summary>
public interface IEncodingDetectorService
{
    /// <summary>
    /// Verilen byte dizisini analiz ederek en olası karakter kodlamasını döndürür.
    /// </summary>
    /// <param name="data">Analiz edilecek ham byte verisi (dosya başından en az 4 KB önerilir)</param>
    /// <returns>Tespit edilen <see cref="Encoding"/>; belirsiz durumda UTF-8 döner.</returns>
    Encoding Detect(byte[] data);
}
