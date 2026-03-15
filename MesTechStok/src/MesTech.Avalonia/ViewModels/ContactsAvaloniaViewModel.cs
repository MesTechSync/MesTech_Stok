using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for CRM Contacts screen — Dalga 11.
/// Will be wired to GetContactsPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class ContactsAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = "Kisiler ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            TotalCount = 0;
            Summary = "Kisiler ekrani hazir. CRM kisi yonetimi, firma eslestirme ve iletisim gecmisi burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
