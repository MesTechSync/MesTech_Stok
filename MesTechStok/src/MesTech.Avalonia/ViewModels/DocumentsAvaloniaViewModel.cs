using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Document Manager screen — Dalga 11.
/// Will be wired to GetDocumentsQuery via MediatR when full migration starts.
/// </summary>
public partial class DocumentsAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string summary = "Belge yonetimi ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            TotalCount = 0;
            Summary = "Belge yonetimi ekrani hazir. Fatura, irsaliye, sozlesme ve diger belge tipleri burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
