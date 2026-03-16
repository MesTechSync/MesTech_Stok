using CommunityToolkit.Mvvm.ComponentModel;
using MesTech.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Concrete ViewModel factory backed by IServiceProvider.
/// All ViewModel types are resolved through constructor injection —
/// no static ServiceLocator access. This class is the ONLY place
/// that holds a reference to IServiceProvider.
/// </summary>
public sealed class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _provider;

    public ViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ObservableObject? Create(string viewName)
    {
        return viewName switch
        {
            // Core views (Dalga 10)
            "Dashboard" => _provider.GetService<DashboardAvaloniaViewModel>(),
            "Leads" => _provider.GetService<LeadsAvaloniaViewModel>(),
            "Kanban" => _provider.GetService<KanbanAvaloniaViewModel>(),
            "ProfitLoss" => _provider.GetService<MesTechStok.Desktop.ViewModels.Finance.ProfitLossViewModel>(),
            "Products" => _provider.GetService<ProductsAvaloniaViewModel>(),
            "Stock" => _provider.GetService<StockAvaloniaViewModel>(),
            "Orders" => _provider.GetService<OrdersAvaloniaViewModel>(),
            "Settings" => _provider.GetService<SettingsAvaloniaViewModel>(),
            // Dalga 11 batch expansion
            "Contacts" => _provider.GetService<ContactsAvaloniaViewModel>(),
            "Employees" => _provider.GetService<EmployeesAvaloniaViewModel>(),
            "LeaveRequests" => _provider.GetService<LeaveRequestsAvaloniaViewModel>(),
            "Documents" => _provider.GetService<DocumentsAvaloniaViewModel>(),
            "Reports" => _provider.GetService<ReportsAvaloniaViewModel>(),
            "Marketplaces" => _provider.GetService<MarketplacesAvaloniaViewModel>(),
            "Expenses" => _provider.GetService<ExpensesAvaloniaViewModel>(),
            "BankAccounts" => _provider.GetService<BankAccountsAvaloniaViewModel>(),
            _ => null
        };
    }
}
