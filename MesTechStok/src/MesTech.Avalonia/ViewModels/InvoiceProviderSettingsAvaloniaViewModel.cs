using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// e-Fatura provider ayarlari ViewModel.
/// 9 provider karti: durum, tip, test sonucu, baglanti testi.
/// P0 XAdES uyarisi.
/// </summary>
public partial class InvoiceProviderSettingsAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string testingProvider = string.Empty;

    public ObservableCollection<ProviderCardItem> Providers { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            await Task.Delay(300);

            Providers.Clear();
            Providers.Add(new()
            {
                Name = "Sovos (Foriba)",
                Status = "Aktif",
                StatusColor = "#388E3C",
                IntegrationType = "Gercek entegrasyon",
                SupportedTypes = "e-Fatura, e-Arsiv",
                LastTestResult = "Basarili",
                LastTestDate = new DateTime(2026, 3, 18, 14, 30, 0)
            });
            Providers.Add(new()
            {
                Name = "GIB Portal",
                Status = "Aktif",
                StatusColor = "#388E3C",
                IntegrationType = "Gercek entegrasyon",
                SupportedTypes = "e-Fatura, e-Arsiv, e-Ihracat",
                LastTestResult = "Basarili",
                LastTestDate = new DateTime(2026, 3, 17, 10, 15, 0)
            });
            Providers.Add(new()
            {
                Name = "Logo e-Fatura",
                Status = "Yapilandirilmadi",
                StatusColor = "#F57C00",
                IntegrationType = "Stub",
                SupportedTypes = "e-Fatura, e-Arsiv",
                LastTestResult = "—",
                LastTestDate = null
            });
            Providers.Add(new()
            {
                Name = "Parasoft e-Fatura",
                Status = "Yapilandirilmadi",
                StatusColor = "#F57C00",
                IntegrationType = "Stub",
                SupportedTypes = "e-Fatura",
                LastTestResult = "—",
                LastTestDate = null
            });
            Providers.Add(new()
            {
                Name = "QNB e-Fatura",
                Status = "Hata",
                StatusColor = "#D32F2F",
                IntegrationType = "Mock",
                SupportedTypes = "e-Fatura, e-Arsiv",
                LastTestResult = "Baglanti hatasi",
                LastTestDate = new DateTime(2026, 3, 15, 9, 0, 0)
            });
            Providers.Add(new()
            {
                Name = "Uyumsoft",
                Status = "Aktif",
                StatusColor = "#388E3C",
                IntegrationType = "Gercek entegrasyon",
                SupportedTypes = "e-Fatura, e-Arsiv",
                LastTestResult = "Basarili",
                LastTestDate = new DateTime(2026, 3, 16, 16, 45, 0)
            });
            Providers.Add(new()
            {
                Name = "Mikro e-Fatura",
                Status = "Yapilandirilmadi",
                StatusColor = "#F57C00",
                IntegrationType = "Stub",
                SupportedTypes = "e-Fatura",
                LastTestResult = "—",
                LastTestDate = null
            });
            Providers.Add(new()
            {
                Name = "Netsis e-Fatura",
                Status = "Yapilandirilmadi",
                StatusColor = "#F57C00",
                IntegrationType = "Mock",
                SupportedTypes = "e-Fatura, e-Arsiv",
                LastTestResult = "—",
                LastTestDate = null
            });
            Providers.Add(new()
            {
                Name = "Edm e-Fatura",
                Status = "Yapilandirilmadi",
                StatusColor = "#F57C00",
                IntegrationType = "Stub",
                SupportedTypes = "e-Fatura",
                LastTestResult = "—",
                LastTestDate = null
            });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Provider bilgileri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task TestConnection(string providerName)
    {
        TestingProvider = providerName;
        var provider = Providers.FirstOrDefault(p => p.Name == providerName);
        if (provider is null) return;

        await Task.Delay(800); // Simulate connection test
        provider.LastTestResult = "Basarili";
        provider.LastTestDate = DateTime.Now;
        TestingProvider = string.Empty;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class ProviderCardItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    private string status = string.Empty;
    public string Status
    {
        get => status;
        set => SetProperty(ref status, value);
    }

    public string StatusColor { get; set; } = "#9CA3AF";
    public string IntegrationType { get; set; } = string.Empty;
    public string SupportedTypes { get; set; } = string.Empty;

    private string lastTestResult = "—";
    public string LastTestResult
    {
        get => lastTestResult;
        set => SetProperty(ref lastTestResult, value);
    }

    private DateTime? lastTestDate;
    public DateTime? LastTestDate
    {
        get => lastTestDate;
        set => SetProperty(ref lastTestDate, value);
    }
}
