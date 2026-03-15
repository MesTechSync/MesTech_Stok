using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Orders screen — Dalga 10.
/// Will be wired to GetOrdersPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class OrdersAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = "Siparis yonetimi ekrani — Dalga 10 sonrasi aktif edilecek.";

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            Summary = "Siparis yonetimi ekrani hazir. Platform siparisleri, kargo takibi ve faturalama burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
