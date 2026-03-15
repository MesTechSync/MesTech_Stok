using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// MainWindow ViewModel — sidebar navigation between 5 PoC views.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string currentViewTitle = "Dashboard";

    private readonly IServiceProvider _services;

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
    }

    [RelayCommand]
    private void NavigateTo(string viewName)
    {
        CurrentView = viewName switch
        {
            // Core views (Dalga 10)
            "Dashboard" => _services.GetService(typeof(DashboardAvaloniaViewModel)) as ObservableObject,
            "Leads" => _services.GetService(typeof(LeadsAvaloniaViewModel)) as ObservableObject,
            "Kanban" => _services.GetService(typeof(KanbanAvaloniaViewModel)) as ObservableObject,
            "ProfitLoss" => _services.GetService(typeof(MesTechStok.Desktop.ViewModels.Finance.ProfitLossViewModel)) as ObservableObject,
            "Products" => _services.GetService(typeof(ProductsAvaloniaViewModel)) as ObservableObject,
            "Stock" => _services.GetService(typeof(StockAvaloniaViewModel)) as ObservableObject,
            "Orders" => _services.GetService(typeof(OrdersAvaloniaViewModel)) as ObservableObject,
            "Settings" => _services.GetService(typeof(SettingsAvaloniaViewModel)) as ObservableObject,
            // Dalga 11 batch expansion
            "Contacts" => _services.GetService(typeof(ContactsAvaloniaViewModel)) as ObservableObject,
            "Employees" => _services.GetService(typeof(EmployeesAvaloniaViewModel)) as ObservableObject,
            "LeaveRequests" => _services.GetService(typeof(LeaveRequestsAvaloniaViewModel)) as ObservableObject,
            "Documents" => _services.GetService(typeof(DocumentsAvaloniaViewModel)) as ObservableObject,
            "Reports" => _services.GetService(typeof(ReportsAvaloniaViewModel)) as ObservableObject,
            "Marketplaces" => _services.GetService(typeof(MarketplacesAvaloniaViewModel)) as ObservableObject,
            "Expenses" => _services.GetService(typeof(ExpensesAvaloniaViewModel)) as ObservableObject,
            "BankAccounts" => _services.GetService(typeof(BankAccountsAvaloniaViewModel)) as ObservableObject,
            _ => CurrentView
        };
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
