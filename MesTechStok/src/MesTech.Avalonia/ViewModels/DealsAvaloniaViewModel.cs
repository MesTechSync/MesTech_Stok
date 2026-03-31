using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class DealsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStage;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string totalAmount = "0 TL";

    public ObservableCollection<DealListItemVm> Deals { get; } = [];
    public string[] StageOptions { get; } = ["Tumu", "Ilk Iletisim", "Teklif Verildi", "Muzakere", "Kazanildi", "Kaybedildi"];

    public DealsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetDealsQuery(_currentUser.TenantId, Page: 1, PageSize: 100));

            Deals.Clear();
            foreach (var deal in result.Items)
            {
                Deals.Add(new DealListItemVm
                {
                    Id = deal.Id,
                    Title = deal.Title,
                    ContactName = deal.ContactName,
                    Amount = deal.Amount,
                    Stage = deal.StageName,
                    Probability = 0,
                    CreatedAt = deal.CreatedAt
                });
            }
            TotalCount = result.TotalCount;
            TotalAmount = $"{Deals.Sum(d => d.Amount):N0} TL";
            IsEmpty = Deals.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Firsatlar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSelectedStageChanged(string? value)
        => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }

    [RelayCommand]
    private async Task Add()
    {
        await _dialog.ShowInfoAsync("Bu özellik yakinda aktif olacak.", "MesTech");
    }
}

public class DealListItemVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public DateTime CreatedAt { get; set; }

    public string AmountDisplay => $"{Amount:N0} TL";
    public string ProbabilityDisplay => $"%{Probability}";
}
