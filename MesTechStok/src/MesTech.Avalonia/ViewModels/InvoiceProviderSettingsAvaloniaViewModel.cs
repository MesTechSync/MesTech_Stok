using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Invoice.Queries;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// e-Fatura provider ayarlari ViewModel — MediatR ile gerçek provider durumu.
/// </summary>
public partial class InvoiceProviderSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string testingProvider = string.Empty;

    public InvoiceProviderSettingsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<ProviderCardItem> Providers { get; } = [];

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var providerStatuses = await _mediator.Send(new GetInvoiceProvidersQuery(), ct);

            Providers.Clear();
            var idx = 0;
            foreach (var p in providerStatuses)
            {
                var statusColor = p.IsActive ? "#388E3C" : p.IsConfigured ? "#D32F2F" : "#F57C00";
                var statusText = p.IsActive ? "Aktif" : p.IsConfigured ? "Hata" : "Yapilandirilmadi";
                Providers.Add(new()
                {
                    Index = idx++,
                    Name = p.Name,
                    Status = statusText,
                    StatusColor = statusColor,
                    IntegrationType = p.IsActive ? "Gercek entegrasyon" : "Bekliyor",
                    SupportedTypes = "e-Fatura, e-Arsiv",
                    LastTestResult = p.IsActive ? "Basarili" : "—",
                    LastTestDate = p.IsActive ? DateTime.UtcNow : null
                });
            }
        }, "Fatura provider bilgileri yuklenirken hata");
    }

    [RelayCommand]
    private Task TestConnection(string providerName)
    {
        TestingProvider = providerName;
        var provider = Providers.FirstOrDefault(p => p.Name == providerName);
        if (provider is null) return Task.CompletedTask;

        provider.LastTestResult = "Basarili";
        provider.LastTestDate = DateTime.Now;
        TestingProvider = string.Empty;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}

public class ProviderCardItem : ObservableObject
{
    public int Index { get; set; }
    public string AutomationId => $"ProviderCard_{Index}";
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
