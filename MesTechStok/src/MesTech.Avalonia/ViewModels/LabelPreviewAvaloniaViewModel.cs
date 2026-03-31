#pragma warning disable CS1998
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;
using MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Label Preview screen — I-05 Siparis/Kargo Celiklestirme.
/// Shows shipping label data with format selection and print/download.
/// </summary>
public partial class LabelPreviewAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    // Label data
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private Guid shipmentId;
    [ObservableProperty] private string trackingNumber = string.Empty;
    [ObservableProperty] private string cargoProvider = string.Empty;
    [ObservableProperty] private string senderName = string.Empty;
    [ObservableProperty] private string senderAddress = string.Empty;
    [ObservableProperty] private string senderPhone = string.Empty;
    [ObservableProperty] private string recipientName = string.Empty;
    [ObservableProperty] private string recipientAddress = string.Empty;
    [ObservableProperty] private string recipientPhone = string.Empty;
    [ObservableProperty] private string recipientCity = string.Empty;
    [ObservableProperty] private int parcelCount = 1;
    [ObservableProperty] private decimal weight;
    [ObservableProperty] private string barcodeText = string.Empty;
    [ObservableProperty] private string selectedFormat = "PDF";

    public List<string> Formats { get; } = ["PDF", "ZPL", "PNG"];

    public LabelPreviewAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        try
        {

            TrackingNumber = "YK-2026032001";
            CargoProvider = "Yurtici Kargo";
            SenderName = "MesTech E-Ticaret A.S.";
            SenderAddress = "Teknopark Istanbul, Pendik";
            SenderPhone = "0216 123 45 67";
            RecipientName = "Ahmet Yilmaz";
            RecipientAddress = "Ataturk Cad. No:42 D:5 Besiktas";
            RecipientPhone = "0532 987 65 43";
            RecipientCity = "Istanbul";
            ParcelCount = 1;
            Weight = 1.2m;
            BarcodeText = TrackingNumber;

            IsEmpty = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Etiket verisi yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new PrintShipmentLabelCommand(
                _currentUser.TenantId, ShipmentId));
            StatusMessage = result.IsSuccess ? "Etiket yaziciya gonderildi." : result.ErrorMessage ?? "Yazdir hatasi";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Yazdir islemi basarisiz: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _mediator.Send(new DownloadShipmentLabelQuery(
                _currentUser.TenantId, ShipmentId, TrackingNumber));
            if (result.LabelData.Length > 0)
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, result.FileName);
                await File.WriteAllBytesAsync(filePath, result.LabelData);
                StatusMessage = $"Etiket indirildi: {filePath}";
            }
            else
            {
                StatusMessage = "Etiket verisi bos — indirilemedi.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Indirme islemi basarisiz: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
