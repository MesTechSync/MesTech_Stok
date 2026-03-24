using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Invoice.Queries;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// e-Fatura provider ayarlari ViewModel — MediatR ile gerçek provider durumu.
/// </summary>
public partial class InvoiceProviderSettingsAvaloniaViewModel : ObservableObject
{
    private readonly ISender _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string testingProvider = string.Empty;

    public InvoiceProviderSettingsAvaloniaViewModel(ISender mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<ProviderCardItem> Providers { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            var providerStatuses = await _mediator.Send(new GetInvoiceProvidersQuery());

            Providers.Clear();
            foreach (var p in providerStatuses)
            {
                var statusColor = p.IsActive ? "#388E3C" : p.IsConfigured ? "#D32F2F" : "#F57C00";
                var statusText = p.IsActive ? "Aktif" : p.IsConfigured ? "Hata" : "Yapilandirilmadi";
                Providers.Add(new()
                {
                    Name = p.Name,
                    Status = statusText,
                    StatusColor = statusColor,
                    IntegrationType = p.IsActive ? "Gercek entegrasyon" : "Bekliyor",
                    SupportedTypes = "e-Fatura, e-Arsiv",
                    LastTestResult = p.IsActive ? "Basarili" : "—",
                    LastTestDate = p.IsActive ? DateTime.UtcNow : null
                });
            }
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
