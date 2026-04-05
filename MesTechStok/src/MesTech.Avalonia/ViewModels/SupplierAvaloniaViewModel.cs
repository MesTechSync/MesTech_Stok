using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tedarikciler ViewModel — tedarikci listesi DataGrid.
/// EMR-12: Enhanced from placeholder to functional view.
/// HH-FIX-supplier: sort + search filter + Excel export added.
/// </summary>
public partial class SupplierAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    private List<SupplierItemDto> _allItems = [];

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

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
                SearchTerm: null), ct);

            _allItems = result.Items.Select(dto => new SupplierItemDto
            {
                SupplierName = dto.Name,
                ContactPerson = string.Empty,
                Phone = dto.Phone ?? string.Empty,
                Email = dto.Email ?? string.Empty,
                City = dto.City ?? string.Empty,
                Balance = dto.CurrentBalance
            }).ToList();

            ApplyFilter();
        }, "Tedarikciler yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allItems.Count > 0)
            ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(s =>
                s.SupplierName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.City.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Phone.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();

        list = SortColumn switch
        {
            "SupplierName" => SortAscending ? [.. list.OrderBy(s => s.SupplierName)] : [.. list.OrderByDescending(s => s.SupplierName)],
            "City"         => SortAscending ? [.. list.OrderBy(s => s.City)]         : [.. list.OrderByDescending(s => s.City)],
            "Balance"      => SortAscending ? [.. list.OrderBy(s => s.Balance)]      : [.. list.OrderByDescending(s => s.Balance)],
            "Phone"        => SortAscending ? [.. list.OrderBy(s => s.Phone)]        : [.. list.OrderByDescending(s => s.Phone)],
            "Email"        => SortAscending ? [.. list.OrderBy(s => s.Email)]        : [.. list.OrderByDescending(s => s.Email)],
            _              => [.. list.OrderBy(s => s.SupplierName)]
        };

        Suppliers.Clear();
        foreach (var item in list)
            Suppliers.Add(item);

        TotalCount = Suppliers.Count;
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "suppliers", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Tedarikciler disa aktarilirken hata");
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
