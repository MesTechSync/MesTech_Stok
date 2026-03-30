namespace MesTech.Application.Features.Shipping.Commands.CreateShipment;

public sealed class CreateShipmentResult
{
    public bool IsSuccess { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CargoBarcode { get; set; }
    public string? ErrorMessage { get; set; }

    public static CreateShipmentResult Succeeded(string trackingNumber, string? cargoBarcode = null)
        => new()
        {
            IsSuccess = true,
            TrackingNumber = trackingNumber,
            CargoBarcode = cargoBarcode
        };

    public static CreateShipmentResult Failed(string error)
        => new()
        {
            IsSuccess = false,
            ErrorMessage = error
        };
}
