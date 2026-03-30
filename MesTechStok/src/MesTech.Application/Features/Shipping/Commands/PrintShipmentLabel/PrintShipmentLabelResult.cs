namespace MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;

/// <summary>
/// Kargo etiketi yazdirma sonucu.
/// </summary>
public sealed class PrintShipmentLabelResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
