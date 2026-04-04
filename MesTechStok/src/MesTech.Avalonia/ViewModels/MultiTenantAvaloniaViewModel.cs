using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Tenant.Queries.GetTenants;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Multi-Tenant Yonetimi ViewModel — tenant listesi + aktif tenant bilgisi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class MultiTenantAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string activeTenantName = "MesTech Ana";
    [ObservableProperty] private string activeTenantId = "tenant-001";

    public ObservableCollection<TenantListItemDto> Tenants { get; } = [];

    public MultiTenantAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetTenantsQuery(), ct);

            Tenants.Clear();
            foreach (var t in result.Items)
            {
                Tenants.Add(new TenantListItemDto
                {
                    Name = t.Name,
                    Database = string.Empty,
                    Status = t.IsActive ? "Aktif" : "Pasif",
                    CreatedAt = string.Empty
                });
            }

            var first = result.Items.FirstOrDefault();
            if (first is not null)
            {
                ActiveTenantName = first.Name;
                ActiveTenantId = first.Id.ToString();
            }
        }, "Tenant verileri yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class TenantListItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
