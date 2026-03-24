using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Label Preview screen — I-05 Siparis/Kargo Celiklestirme.
/// Shows shipping label data with format selection and print/download.
/// </summary>
public partial class LabelPreviewAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // Label data
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

    public LabelPreviewAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        try
        {
            await Task.Delay(100);

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
            await Task.Delay(500); // Simulate print
            // In production: send to printer based on SelectedFormat
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
            await Task.Delay(300); // Simulate download
            // In production: save file based on SelectedFormat
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
