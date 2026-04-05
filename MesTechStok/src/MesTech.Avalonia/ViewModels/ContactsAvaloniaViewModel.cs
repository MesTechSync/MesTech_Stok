using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// CRM Contacts ViewModel — wired to GetContactsPagedQuery via MediatR.
/// </summary>
public partial class ContactsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    private List<ContactListDto> _allContacts = [];

    [ObservableProperty] private ObservableCollection<ContactListDto> contacts = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int currentPage = 1;

    // Sort
    [ObservableProperty] private string sortColumn = "date";
    [ObservableProperty] private bool sortAscending = false;

    public ContactsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Kisiler";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetContactsPagedQuery(
                    _currentUser.TenantId,
                    CurrentPage,
                    Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText),
                ct);

            _allContacts = result.Contacts.ToList();
            TotalCount = result.TotalCount;
            ApplyFilters();
        }, "Kisiler yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allContacts.AsEnumerable();

        // Sort
        filtered = SortColumn switch
        {
            "name"  => SortAscending ? filtered.OrderBy(c => c.FullName)   : filtered.OrderByDescending(c => c.FullName),
            "email" => SortAscending ? filtered.OrderBy(c => c.Email)      : filtered.OrderByDescending(c => c.Email),
            "date"  => SortAscending ? filtered.OrderBy(c => c.CreatedAt)  : filtered.OrderByDescending(c => c.CreatedAt),
            _       => filtered.OrderByDescending(c => c.CreatedAt)
        };

        Contacts = new ObservableCollection<ContactListDto>(filtered);
        Summary = $"Toplam {TotalCount} kisi";
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
        ApplyFilters();
    }

    [RelayCommand]
    private async Task Add()
    {
        // G98 FIX: Wire to CreateContactCommand when DEV1 implements handler.
        // Current: local-only insert (not persisted to DB).
        // Target: await _mediator.Send(new CreateContactCommand(...), CancellationToken);
        var newContact = new ContactListDto
        {
            ContactId = Guid.NewGuid(),
            FullName = "Yeni Kisi",
            CreatedAt = DateTime.UtcNow
        };
        Contacts.Insert(0, newContact);
        TotalCount = Contacts.Count;
        Summary = $"Toplam {TotalCount} kisi";
        IsEmpty = false;
        await Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task NextPage()
    {
        CurrentPage++;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1) CurrentPage--;
        await LoadAsync();
    }
}
