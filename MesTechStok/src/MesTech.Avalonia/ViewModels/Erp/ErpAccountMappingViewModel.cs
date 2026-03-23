using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Erp;

/// <summary>
/// ERP Account Mapping ViewModel — E-02.
/// Split panel: MesTech cari hesaplar (left) + ERP cari hesaplar (right).
/// Middle "Esle" button maps selected items.
/// Bottom: mapped pairs DataGrid showing MesTech Account <-> ERP Account.
/// </summary>
public partial class ErpAccountMappingViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
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

    public ErpAccountMappingViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    partial void OnMesTechSearchTextChanged(string value) => ApplyMesTechFilter();
    partial void OnErpSearchTextChanged(string value) => ApplyErpFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR queries

            // Demo MesTech accounts
            _allMesTechAccounts =
            [
                new() { Code = "120.01", Name = "Alicilar - Yurtici", AccountType = "Alici" },
                new() { Code = "120.02", Name = "Alicilar - Yurtdisi", AccountType = "Alici" },
                new() { Code = "320.01", Name = "Saticilar - Yurtici", AccountType = "Satici" },
                new() { Code = "320.02", Name = "Saticilar - Yurtdisi", AccountType = "Satici" },
                new() { Code = "100.01", Name = "Kasa Hesabi - TL", AccountType = "Kasa" },
                new() { Code = "102.01", Name = "Banka Hesabi - Garanti", AccountType = "Banka" },
                new() { Code = "102.02", Name = "Banka Hesabi - Isbank", AccountType = "Banka" },
                new() { Code = "153.01", Name = "Ticari Mallar", AccountType = "Stok" },
                new() { Code = "600.01", Name = "Yurtici Satislar", AccountType = "Gelir" },
                new() { Code = "600.02", Name = "Yurtdisi Satislar", AccountType = "Gelir" },
            ];

            // Demo ERP accounts
            _allErpAccounts =
            [
                new() { Code = "ERP-120-001", Name = "Musteriler Genel", AccountType = "Alici" },
                new() { Code = "ERP-120-002", Name = "Musteriler Ihracat", AccountType = "Alici" },
                new() { Code = "ERP-320-001", Name = "Tedarikciler Genel", AccountType = "Satici" },
                new() { Code = "ERP-320-002", Name = "Tedarikciler Ithalat", AccountType = "Satici" },
                new() { Code = "ERP-100-001", Name = "Ana Kasa", AccountType = "Kasa" },
                new() { Code = "ERP-102-001", Name = "Garanti Bankasi", AccountType = "Banka" },
                new() { Code = "ERP-102-002", Name = "Is Bankasi", AccountType = "Banka" },
                new() { Code = "ERP-153-001", Name = "Stok Hesabi", AccountType = "Stok" },
                new() { Code = "ERP-600-001", Name = "Satis Gelirleri", AccountType = "Gelir" },
                new() { Code = "ERP-600-002", Name = "Ihracat Gelirleri", AccountType = "Gelir" },
            ];

            ApplyMesTechFilter();
            ApplyErpFilter();

            // Demo mapped pairs
            MappedPairs.Clear();
            MappedPairs.Add(new MappedPairItem
            {
                MesTechCode = "120.01",
                MesTechName = "Alicilar - Yurtici",
                ErpCode = "ERP-120-001",
                ErpName = "Musteriler Genel",
                MappedDate = "23.03.2026 14:30"
            });
            MappedPairs.Add(new MappedPairItem
            {
                MesTechCode = "320.01",
                MesTechName = "Saticilar - Yurtici",
                ErpCode = "ERP-320-001",
                ErpName = "Tedarikciler Genel",
                MappedDate = "23.03.2026 14:32"
            });
            MappedPairs.Add(new MappedPairItem
            {
                MesTechCode = "102.01",
                MesTechName = "Banka Hesabi - Garanti",
                ErpCode = "ERP-102-001",
                ErpName = "Garanti Bankasi",
                MappedDate = "22.03.2026 10:15"
            });

            MappedCount = MappedPairs.Count;
            IsEmpty = MesTechAccounts.Count == 0 && ErpAccounts.Count == 0;
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
