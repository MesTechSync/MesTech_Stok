using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tedarikciler ViewModel — tedarikci listesi DataGrid.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class SupplierAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<SupplierItemDto> Suppliers { get; } = [];

    public SupplierAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetSuppliersCrmQuery(
                TenantId: _currentUser.TenantId,
                SearchTerm: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText), ct);

            Suppliers.Clear();
            foreach (var dto in result.Items)
            {
                Suppliers.Add(new SupplierItemDto
                {
                    SupplierName = dto.Name,
                    ContactPerson = string.Empty,
                    Phone = dto.Phone ?? string.Empty,
                    Email = dto.Email ?? string.Empty,
                    City = dto.City ?? string.Empty,
                    Balance = dto.CurrentBalance
                });
            }

            TotalCount = result.TotalCount;
            IsEmpty = TotalCount == 0;
        }, "Tedarikciler yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class SupplierItemDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
