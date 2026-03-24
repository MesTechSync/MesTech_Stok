using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for HR Employees screen — Dalga 11.
/// Will be wired to GetEmployeesPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class EmployeesAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string summary = "Calisan yonetimi ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            TotalCount = 0;
            Summary = "Calisan yonetimi ekrani hazir. Personel kaydi, departman atama ve organizasyon semasi burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
