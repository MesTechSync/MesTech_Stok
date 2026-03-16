using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Avalonia.Services;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// MainWindow ViewModel — sidebar navigation between views.
/// Uses IViewModelFactory (proper DI) instead of raw IServiceProvider (ServiceLocator).
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string currentViewTitle = "Dashboard";

    private readonly IViewModelFactory _viewModelFactory;

    public MainWindowViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    [RelayCommand]
    private void NavigateTo(string viewName)
    {
        var resolved = _viewModelFactory.Create(viewName);
        if (resolved is not null)
            CurrentView = resolved;

        CurrentViewTitle = viewName switch
        {
            // Core views (Dalga 10)
            "Dashboard" => "Kontrol Paneli",
            "Leads" => "Potansiyel Musteriler",
            "Kanban" => "Firsatlar — Kanban",
            "ProfitLoss" => "Kar / Zarar Raporu",
            "Products" => "Urunler",
            "Stock" => "Stok Yonetimi",
            "Orders" => "Siparis Yonetimi",
            "Settings" => "Ayarlar",
            // Dalga 11 batch expansion
            "Contacts" => "CRM Kisiler",
            "Employees" => "Calisanlar",
            "LeaveRequests" => "Izin Talepleri",
            "Documents" => "Belge Yonetimi",
            "Reports" => "Raporlar",
            "Marketplaces" => "Pazaryerleri",
            "Expenses" => "Giderler",
            "BankAccounts" => "Banka Hesaplari",
            _ => CurrentViewTitle
        };
    }
}
