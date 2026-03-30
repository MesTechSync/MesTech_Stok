using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class ContactAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ContactItemVm> Contacts { get; } = [];

    public ContactAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(
                new GetContactsPagedQuery(tenantId, Search: SearchText), CancellationToken);

            Contacts.Clear();
            foreach (var c in result.Contacts)
            {
                Contacts.Add(new ContactItemVm
                {
                    Id = c.ContactId,
                    FullName = c.FullName,
                    Company = c.Company,
                    Email = c.Email,
                    Phone = c.Phone,
                });
            }
            TotalCount = result.TotalCount;
            IsEmpty = Contacts.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kisiler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void Add()
    {
        // TODO: Navigate to contact create form or show dialog
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class ContactItemVm
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string Type { get; set; } = string.Empty;
}
