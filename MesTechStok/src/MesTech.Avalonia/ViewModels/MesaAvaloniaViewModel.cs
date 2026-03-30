using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class MesaAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // MESA AI Status
    [ObservableProperty] private string aiStatus = "Bagli";
    [ObservableProperty] private string modelVersion = "v2.1";
    [ObservableProperty] private int predictionCount;
    [ObservableProperty] private string accuracy = "%0";
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<MesaPredictionVm> RecentPredictions { get; } = [];
    public ObservableCollection<MesaToolVm> AiTools { get; } = [];

    public MesaAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {

            AiStatus = "Bagli";
            ModelVersion = "v2.1";
            PredictionCount = 1247;
            Accuracy = "%94.3";
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");

            AiTools.Clear();
            AiTools.Add(new MesaToolVm { Name = "Stok Tahmin", Description = "Gelecek 30 gun stok ihtiyaci tahmini", Status = "Aktif", Icon = "S" });
            AiTools.Add(new MesaToolVm { Name = "Fiyat Optimizasyonu", Description = "Rekabet analizi ile fiyat onerisi", Status = "Aktif", Icon = "F" });
            AiTools.Add(new MesaToolVm { Name = "Anomali Tespit", Description = "Siparis ve stok anomali alarmlari", Status = "Aktif", Icon = "A" });
            AiTools.Add(new MesaToolVm { Name = "Musteri Segmentasyon", Description = "RFM bazli musteri gruplama", Status = "Beta", Icon = "M" });

            RecentPredictions.Clear();
            RecentPredictions.Add(new MesaPredictionVm { Tool = "Stok Tahmin", Result = "Urun A — 30 gun icinde 250 adet ihtiyac", Confidence = "%96", CreatedAt = DateTime.Now.AddHours(-1) });
            RecentPredictions.Add(new MesaPredictionVm { Tool = "Fiyat Optimizasyonu", Result = "Urun B — Onerilen fiyat: 149.90 TL", Confidence = "%89", CreatedAt = DateTime.Now.AddHours(-3) });
            RecentPredictions.Add(new MesaPredictionVm { Tool = "Anomali Tespit", Result = "Depo-2 stok seviyesi normalin %40 altinda", Confidence = "%92", CreatedAt = DateTime.Now.AddHours(-6) });

            IsEmpty = AiTools.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"MESA yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class MesaToolVm
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Aktif";
    public string Icon { get; set; } = "?";
}

public class MesaPredictionVm
{
    public string Tool { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Confidence { get; set; } = "%0";
    public DateTime CreatedAt { get; set; }

    public string TimeDisplay => CreatedAt.ToString("HH:mm");
}
