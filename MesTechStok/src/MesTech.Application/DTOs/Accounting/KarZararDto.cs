namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Kar Zarar data transfer object.
/// </summary>
public class KarZararDto
{
    public decimal ToplamGelir { get; set; }
    public decimal ToplamGider { get; set; }
    public decimal NetKar { get; set; }
    public DateTime DönemBasi { get; set; }
    public DateTime DönemSonu { get; set; }
}
