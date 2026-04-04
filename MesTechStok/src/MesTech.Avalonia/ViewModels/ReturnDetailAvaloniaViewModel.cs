using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Return Detail screen — I-05 Siparis/Kargo Celiklestirme.
/// Shows return detail with timeline, approve/reject commands.
/// Uses ReturnStatus and ReturnReason enums from Domain.
/// </summary>
public partial class ReturnDetailAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    private Guid _returnId;

    [ObservableProperty] private string searchText = string.Empty;
    private List<ReturnSummaryItemDto> _allReturns = [];
    public ObservableCollection<ReturnSummaryItemDto> Returns { get; } = [];

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

    public ReturnDetailAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
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
            var returns = await _mediator.Send(new GetReturnListQuery(_currentUser.TenantId, 100));

            _allReturns = returns.Select(r => new ReturnSummaryItemDto
            {
                Id = r.Id,
                IadeNo = r.Id.ToString()[..8].ToUpperInvariant(),
                SiparisNo = r.OrderNumber ?? string.Empty,
                Sebep = r.Reason ?? string.Empty,
                Durum = r.Status,
                Tutar = r.RefundAmount,
                TalepTarihi = r.CreatedAt
            }).ToList();

            ApplyFilter();

            if (returns.Count > 0)
            {
                var r = returns[0];
                _returnId = r.Id;
                IadeNo = r.Id.ToString()[..8].ToUpperInvariant();
                SiparisNo = r.OrderNumber ?? string.Empty;
                Tutar = r.RefundAmount;
                Sebep = r.Reason ?? string.Empty;
                Durum = r.Status;
                TalepTarihi = r.CreatedAt;
            }
            else
            {
                IsEmpty = true;
            }

            if (returns.Count > 0)
            {
                Timeline.Clear();
                Timeline.Add(new() { Step = "Talep Olusturuldu", Date = new DateTime(2026, 3, 18, 10, 30, 0), IsCompleted = true, Description = "Musteri iade talebi olusturdu" });
                Timeline.Add(new() { Step = "Onay Bekliyor", Date = null, IsCompleted = false, IsCurrent = true, Description = "Iade talebi inceleniyor" });
                Timeline.Add(new() { Step = "Kargo Bekleniyor", Date = null, IsCompleted = false, Description = "Urun kargoya verilecek" });
                Timeline.Add(new() { Step = "Iade Tamamlandi", Date = null, IsCompleted = false, Description = "Iade sureci tamamlanacak" });
            }
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
            var result = await _mediator.Send(new ApproveReturnCommand(_returnId));
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage ?? "Onay basarisiz");

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
            var result = await _mediator.Send(new RejectReturnCommand(_returnId, SelectedRejectReason));
            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage ?? "Red basarisiz");

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

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Returns.Clear();

        var filtered = _allReturns.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(r =>
                r.SiparisNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Sebep.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Durum.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.IadeNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
            Returns.Add(item);

        IsEmpty = Returns.Count == 0 && _allReturns.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class ReturnSummaryItemDto
{
    public Guid Id { get; set; }
    public string IadeNo { get; set; } = string.Empty;
    public string SiparisNo { get; set; } = string.Empty;
    public string Sebep { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public DateTime TalepTarihi { get; set; }
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
