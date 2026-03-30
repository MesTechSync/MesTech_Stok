using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// CRM Contacts ViewModel — wired to GetContactsPagedQuery via MediatR.
/// </summary>
public partial class ContactsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private ObservableCollection<ContactListDto> contacts = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int currentPage = 1;

    public ContactsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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

            Contacts = new ObservableCollection<ContactListDto>(result.Contacts);
            TotalCount = result.TotalCount;
            Summary = $"Toplam {TotalCount} kisi";
            IsEmpty = TotalCount == 0;
        }, "Kisiler yuklenirken hata");
    }

    [RelayCommand]
    private async Task Add()
    {
        // NAV: Open contact create dialog
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
