using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Erp;

/// <summary>
/// ERP Account Mapping ViewModel — E-02.
/// Split panel: MesTech cari hesaplar (left) + ERP cari hesaplar (right).
/// Middle "Esle" button maps selected items.
/// Bottom: mapped pairs DataGrid showing MesTech Account <-> ERP Account.
/// </summary>
public partial class ErpAccountMappingViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int mappedCount;

    // Search
    [ObservableProperty] private string mesTechSearchText = string.Empty;
    [ObservableProperty] private string erpSearchText = string.Empty;

    // Selection
    [ObservableProperty] private AccountItem? selectedMesTechAccount;
    [ObservableProperty] private AccountItem? selectedErpAccount;
    [ObservableProperty] private MappedPairItem? selectedMappedPair;

    // Data
    public ObservableCollection<AccountItem> MesTechAccounts { get; } = [];
    public ObservableCollection<AccountItem> ErpAccounts { get; } = [];
    public ObservableCollection<MappedPairItem> MappedPairs { get; } = [];

    private List<AccountItem> _allMesTechAccounts = [];
    private List<AccountItem> _allErpAccounts = [];

    private readonly ICurrentUserService _currentUser;

    public ErpAccountMappingViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    partial void OnMesTechSearchTextChanged(string value) => ApplyMesTechFilter();
    partial void OnErpSearchTextChanged(string value) => ApplyErpFilter();

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var mappings = await _mediator.Send(new GetErpAccountMappingsQuery(_currentUser.TenantId));

            _allMesTechAccounts = mappings.Select(m => new AccountItem
            {
                Code = m.MesTechAccountCode,
                Name = m.MesTechAccountName,
                AccountType = m.MesTechAccountType
            }).DistinctBy(a => a.Code).ToList();

            _allErpAccounts = mappings.Select(m => new AccountItem
            {
                Code = m.ErpAccountCode,
                Name = m.ErpAccountName,
                AccountType = m.MesTechAccountType
            }).DistinctBy(a => a.Code).ToList();

            ApplyMesTechFilter();
            ApplyErpFilter();

            MappedPairs.Clear();
            foreach (var m in mappings.Where(m => m.IsActive))
            {
                MappedPairs.Add(new MappedPairItem
                {
                    MesTechCode = m.MesTechAccountCode,
                    MesTechName = m.MesTechAccountName,
                    ErpCode = m.ErpAccountCode,
                    ErpName = m.ErpAccountName,
                    MappedDate = m.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? m.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                });
            }

            MappedCount = MappedPairs.Count;
            IsEmpty = _allMesTechAccounts.Count == 0 && _allErpAccounts.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Hesap verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyMesTechFilter()
    {
        MesTechAccounts.Clear();
        var filtered = _allMesTechAccounts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(MesTechSearchText) && MesTechSearchText.Length >= 2)
        {
            var search = MesTechSearchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var item in filtered)
            MesTechAccounts.Add(item);
    }

    private void ApplyErpFilter()
    {
        ErpAccounts.Clear();
        var filtered = _allErpAccounts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(ErpSearchText) && ErpSearchText.Length >= 2)
        {
            var search = ErpSearchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        foreach (var item in filtered)
            ErpAccounts.Add(item);
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private Task MapAccounts()
    {
        if (SelectedMesTechAccount is null || SelectedErpAccount is null)
            return Task.CompletedTask;

        // Check for duplicate mapping
        var alreadyMapped = MappedPairs.Any(p =>
            p.MesTechCode == SelectedMesTechAccount.Code ||
            p.ErpCode == SelectedErpAccount.Code);

        if (alreadyMapped)
        {
            ErrorMessage = "Bu hesap zaten eslesmis. Once mevcut eslestirmeyi kaldirin.";
            return Task.CompletedTask;
        }

        MappedPairs.Add(new MappedPairItem
        {
            MesTechCode = SelectedMesTechAccount.Code,
            MesTechName = SelectedMesTechAccount.Name,
            ErpCode = SelectedErpAccount.Code,
            ErpName = SelectedErpAccount.Name,
            MappedDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
        });

        MappedCount = MappedPairs.Count;
        ErrorMessage = string.Empty;

        // Will be replaced with MediatR command to persist mapping
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RemoveMapping()
    {
        if (SelectedMappedPair is null)
            return Task.CompletedTask;

        MappedPairs.Remove(SelectedMappedPair);
        MappedCount = MappedPairs.Count;
        SelectedMappedPair = null;

        // Will be replaced with MediatR command to remove mapping
        return Task.CompletedTask;
    }
}

/// <summary>
/// Account list item used in both MesTech and ERP account DataGrids.
/// </summary>
public class AccountItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
}

/// <summary>
/// Mapped pair item showing MesTech Account <-> ERP Account relationship.
/// </summary>
public class MappedPairItem
{
    public string MesTechCode { get; set; } = string.Empty;
    public string MesTechName { get; set; } = string.Empty;
    public string ErpCode { get; set; } = string.Empty;
    public string ErpName { get; set; } = string.Empty;
    public string MappedDate { get; set; } = string.Empty;
}
