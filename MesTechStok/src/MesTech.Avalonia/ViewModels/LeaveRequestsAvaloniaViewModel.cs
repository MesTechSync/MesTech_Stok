using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for HR Leave Requests screen — Dalga 11.
/// Will be wired to GetLeaveRequestsQuery via MediatR when full migration starts.
/// </summary>
public partial class LeaveRequestsAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string summary = "Izin talepleri ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            TotalCount = 0;
            Summary = "Izin talepleri ekrani hazir. Izin basvurusu, onay sureci ve yillik izin takibi burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
