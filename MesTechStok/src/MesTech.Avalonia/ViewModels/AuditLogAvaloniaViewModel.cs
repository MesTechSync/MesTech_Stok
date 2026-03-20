using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Audit Log ViewModel — filterable action log with detail view and CSV export.
/// İ-11 Görev 4C: System audit trail with mock data.
/// </summary>
public partial class AuditLogAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string exportMessage = string.Empty;
    [ObservableProperty] private bool isExported;

    // Filters
    [ObservableProperty] private string selectedUser = "Tumu";
    [ObservableProperty] private string selectedAction = "Tumu";
    [ObservableProperty] private DateTimeOffset startDate = new(new DateTime(2026, 3, 1));
    [ObservableProperty] private DateTimeOffset endDate = new(new DateTime(2026, 3, 20));

    // Detail panel
    [ObservableProperty] private bool isDetailVisible;
    [ObservableProperty] private AuditLogEntry? selectedEntry;

    public ObservableCollection<string> Users { get; } = new()
    {
        "Tumu", "admin@mestech.com", "dev1@mestech.com", "dev2@mestech.com",
        "operator@mestech.com", "muhasebe@mestech.com"
    };

    public ObservableCollection<string> ActionTypes { get; } = new()
    {
        "Tumu", "Create", "Update", "Delete", "Login", "Export"
    };

    public ObservableCollection<AuditLogEntry> LogEntries { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate loading

            LogEntries.Clear();
            LogEntries.Add(new AuditLogEntry("20.03.2026 14:32", "admin@mestech.com", "Update", "Product", "PRD-4521", "Fiyat guncellendi: 149.90 → 159.90 TL", "Fiyat: 149.90 TL", "Fiyat: 159.90 TL"));
            LogEntries.Add(new AuditLogEntry("20.03.2026 13:15", "operator@mestech.com", "Create", "Order", "ORD-8834", "Yeni siparis olusturuldu", "-", "Siparis: ORD-8834, Tutar: 2.450 TL"));
            LogEntries.Add(new AuditLogEntry("20.03.2026 11:45", "dev1@mestech.com", "Delete", "Product", "PRD-1102", "Urun silindi: Test Urun", "Urun: Test Urun, SKU: TST-001", "-"));
            LogEntries.Add(new AuditLogEntry("20.03.2026 10:20", "admin@mestech.com", "Login", "User", "USR-001", "Basarili giris", "-", "IP: 192.168.1.45, Cihaz: Chrome/Win"));
            LogEntries.Add(new AuditLogEntry("19.03.2026 17:30", "muhasebe@mestech.com", "Export", "Report", "RPT-056", "Stok raporu disari aktarildi", "-", "Format: Excel, Boyut: 2.4 MB"));
            LogEntries.Add(new AuditLogEntry("19.03.2026 16:10", "admin@mestech.com", "Update", "Settings", "SYS-001", "API URL degistirildi", "URL: https://old-api.mestech.com", "URL: https://api.mestech.com/v2"));
            LogEntries.Add(new AuditLogEntry("19.03.2026 14:55", "dev2@mestech.com", "Create", "Product", "PRD-4522", "Yeni urun eklendi: Bluetooth Kulaklik", "-", "Urun: Bluetooth Kulaklik, SKU: BT-100"));
            LogEntries.Add(new AuditLogEntry("19.03.2026 12:00", "operator@mestech.com", "Update", "Order", "ORD-8820", "Kargo durumu guncellendi", "Durum: Hazirlaniyor", "Durum: Kargoya Verildi"));
            LogEntries.Add(new AuditLogEntry("18.03.2026 15:40", "admin@mestech.com", "Update", "User", "USR-003", "Kullanici rol degisikligi", "Rol: Operator", "Rol: Admin"));
            LogEntries.Add(new AuditLogEntry("18.03.2026 09:30", "dev1@mestech.com", "Login", "User", "USR-002", "Basarisiz giris denemesi", "-", "IP: 10.0.0.5, Sebep: Yanlis sifre"));

            IsEmpty = LogEntries.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Denetim kayitlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowDetail(AuditLogEntry? entry)
    {
        SelectedEntry = entry;
        IsDetailVisible = entry != null;
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedEntry = null;
        IsDetailVisible = false;
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        IsExported = false;
        ExportMessage = string.Empty;
        IsLoading = true;
        try
        {
            await Task.Delay(600); // Simulate export
            ExportMessage = $"CSV dosyasi basariyla olusturuldu. ({LogEntries.Count} kayit)";
            IsExported = true;
        }
        catch (Exception ex)
        {
            ExportMessage = $"Disari aktarma hatasi: {ex.Message}";
            IsExported = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class AuditLogEntry
{
    public string Timestamp { get; }
    public string User { get; }
    public string Action { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public string Summary { get; }
    public string OldValues { get; }
    public string NewValues { get; }

    public AuditLogEntry(string timestamp, string user, string action, string entityType,
        string entityId, string summary, string oldValues, string newValues)
    {
        Timestamp = timestamp;
        User = user;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Summary = summary;
        OldValues = oldValues;
        NewValues = newValues;
    }
}
