using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Return Detail screen — I-05 Siparis/Kargo Celiklestirme.
/// Shows return detail with timeline, approve/reject commands.
/// Uses ReturnStatus and ReturnReason enums from Domain.
/// </summary>
public partial class ReturnDetailAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // Return info
    [ObservableProperty] private string iadeNo = string.Empty;
    [ObservableProperty] private string siparisNo = string.Empty;
    [ObservableProperty] private string musteri = string.Empty;
    [ObservableProperty] private string platform = string.Empty;
    [ObservableProperty] private decimal tutar;
    [ObservableProperty] private string sebep = string.Empty;
    [ObservableProperty] private string aciklama = string.Empty;
    [ObservableProperty] private string durum = string.Empty;
    [ObservableProperty] private DateTime talepTarihi;
    [ObservableProperty] private string selectedRejectReason = "Diger";

    public ObservableCollection<ReturnTimelineStepDto> Timeline { get; } = [];

    public ObservableCollection<string> RejectReasons { get; } =
    [
        "Iade suresi dolmus",
        "Urun hasarli iade edilmis",
        "Urun kullanilmis",
        "Fatura/etiket eksik",
        "Diger"
    ];

    public ReturnDetailAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(120);

            IadeNo = "IAD-2001";
            SiparisNo = "SIP-1001";
            Musteri = "Ahmet Yilmaz";
            Platform = "Trendyol";
            Tutar = 249.90m;
            Sebep = "Hatali Urun";
            Aciklama = "Urun kutusundan hasarli cikti, ambalaj ezilmis.";
            Durum = "Beklemede";
            TalepTarihi = new DateTime(2026, 3, 18);

            Timeline.Clear();
            Timeline.Add(new() { Step = "Talep Olusturuldu", Date = new DateTime(2026, 3, 18, 10, 30, 0), IsCompleted = true, Description = "Musteri iade talebi olusturdu" });
            Timeline.Add(new() { Step = "Onay Bekliyor", Date = null, IsCompleted = false, IsCurrent = true, Description = "Iade talebi inceleniyor" });
            Timeline.Add(new() { Step = "Kargo Bekleniyor", Date = null, IsCompleted = false, Description = "Urun kargoya verilecek" });
            Timeline.Add(new() { Step = "Iade Tamamlandi", Date = null, IsCompleted = false, Description = "Iade sureci tamamlanacak" });

            IsEmpty = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Iade detaylari yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(300); // Simulate API call
            Durum = "Onaylandi";

            // Update timeline
            if (Timeline.Count >= 2)
            {
                Timeline[1].IsCompleted = true;
                Timeline[1].IsCurrent = false;
                Timeline[1].Date = DateTime.Now;
                if (Timeline.Count >= 3)
                    Timeline[2].IsCurrent = true;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Onay islemi basarisiz: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RejectAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(300); // Simulate API call
            Durum = "Reddedildi";

            if (Timeline.Count >= 2)
            {
                Timeline[1].IsCompleted = true;
                Timeline[1].IsCurrent = false;
                Timeline[1].Date = DateTime.Now;
                Timeline[1].Description = $"Reddedildi: {SelectedRejectReason}";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Red islemi basarisiz: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class ReturnTimelineStepDto : ObservableObject
{
    private bool _isCompleted;
    public bool IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }

    private bool _isCurrent;
    public bool IsCurrent { get => _isCurrent; set => SetProperty(ref _isCurrent, value); }

    private DateTime? _date;
    public DateTime? Date { get => _date; set => SetProperty(ref _date, value); }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    public string Step { get; set; } = string.Empty;
}
