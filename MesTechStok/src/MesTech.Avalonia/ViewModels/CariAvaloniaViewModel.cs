using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Cari Hesap (Account) ViewModel — wired to GetCounterpartiesQuery via MediatR.
/// G033: Task.Delay mock replaced with real mediator.Send call.
/// DataGrid: Ad, Tip, Borc, Alacak, Bakiye with Musteri/Tedarikci filter.
/// </summary>
public partial class CariAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<CariItemDto> Accounts { get; } = [];

    public ObservableCollection<string> AccountTypes { get; } =
    [
        "Tumu", "Musteri", "Tedarikci"
    ];

    private List<CariItemDto> _allAccounts = [];

    public CariAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var results = await _mediator.Send(
                new GetCounterpartiesQuery(_currentUser.TenantId),
                CancellationToken);

            _allAccounts = results.Select(c => new CariItemDto
            {
                Name = c.Name,
                Type = c.CounterpartyType switch
                {
                    "Customer" => "Musteri",
                    "Supplier" => "Tedarikci",
                    _ => c.CounterpartyType
                },
                Debt = 0m,
                Credit = 0m,
                Balance = 0m
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Cari hesaplar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allAccounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
        {
            filtered = filtered.Where(a => a.Type == SelectedType);
        }

        Accounts.Clear();
        foreach (var item in filtered)
            Accounts.Add(item);

        TotalCount = Accounts.Count;
        IsEmpty = Accounts.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allAccounts.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedTypeChanged(string value)
    {
        if (_allAccounts.Count > 0)
            ApplyFilters();
    }
}

public class CariItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Debt { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}
