using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MediatR;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.Views;
using global::MesTech.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia;

/// <summary>
/// Avalonia App entry point with IHost DI — proper dependency injection host.
/// Replaces the former static ServiceLocator (App.Services) pattern.
/// Uses SAME AddInfrastructure() registration as WPF Desktop.
/// Domain + Application + Infrastructure = ZERO CHANGES.
///
/// EMR-02: Startup flow → WelcomeWindow → LoginWindow → MainWindow (DI).
/// CreateMainWindow() provides DI-resolved MainWindow for LoginWindow to call.
/// </summary>
public partial class App : global::Avalonia.Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Creates a DI-resolved MainWindow with ViewModel and auto-navigates to Dashboard.
    /// Called by LoginWindow after successful authentication.
    /// </summary>
    public MainWindow CreateMainWindow()
    {
        var mainVm = _host!.Services.GetRequiredService<MainWindowViewModel>();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = mainVm;
        mainVm.NavigateToCommand.Execute("Dashboard");
        return mainWindow;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((ctx, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var configuration = ctx.Configuration;

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // === SAME Infrastructure DI as WPF Desktop — ZERO CHANGES ===
                services.AddInfrastructure(configuration);

                // MediatR — Application CQRS handlers
                services.AddMediatR(cfg =>
                    cfg.RegisterServicesFromAssembly(
                        typeof(global::MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly));

                // === Avalonia-specific services ===
                services.AddSingleton<IDialogService, ConsoleDialogService>();
                services.AddSingleton<IViewModelFactory, ViewModelFactory>();

                // Views — registered for DI resolution
                services.AddTransient<MainWindow>();

                // ViewModels — Core (Dalga 10)
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<DashboardAvaloniaViewModel>();
                services.AddTransient<LeadsAvaloniaViewModel>();
                services.AddTransient<KanbanAvaloniaViewModel>();
                services.AddTransient<ProductsAvaloniaViewModel>();
                services.AddTransient<StockAvaloniaViewModel>();
                services.AddTransient<OrdersAvaloniaViewModel>();
                services.AddTransient<SettingsAvaloniaViewModel>();
                services.AddTransient<LoginAvaloniaViewModel>();
                services.AddTransient<CategoryAvaloniaViewModel>();
                // ProfitLossViewModel — compile-linked from WPF Desktop, zero changes
                services.AddTransient<MesTechStok.Desktop.ViewModels.Finance.ProfitLossViewModel>();

                // ViewModels — Dalga 11 batch expansion
                services.AddTransient<ContactsAvaloniaViewModel>();
                services.AddTransient<EmployeesAvaloniaViewModel>();
                services.AddTransient<LeaveRequestsAvaloniaViewModel>();
                services.AddTransient<DocumentsAvaloniaViewModel>();
                services.AddTransient<ReportsAvaloniaViewModel>();
                services.AddTransient<MarketplacesAvaloniaViewModel>();
                services.AddTransient<ExpensesAvaloniaViewModel>();
                services.AddTransient<BankAccountsAvaloniaViewModel>();

                // ViewModels — Dalga 14+15 functional views
                services.AddTransient<InventoryAvaloniaViewModel>();
                services.AddTransient<InvoiceManagementAvaloniaViewModel>();
                services.AddTransient<CustomerAvaloniaViewModel>();
                services.AddTransient<CariHesaplarAvaloniaViewModel>();
                services.AddTransient<SyncStatusAvaloniaViewModel>();
                services.AddTransient<StockMovementAvaloniaViewModel>();
                services.AddTransient<CargoTrackingAvaloniaViewModel>();

                // ViewModels — Dalga 17 batch (T007: 49 ViewModel iskelet)
                services.AddTransient<AboutAvaloniaViewModel>();
                services.AddTransient<AccountingDashboardAvaloniaViewModel>();
                services.AddTransient<ActivityAvaloniaViewModel>();
                services.AddTransient<AmazonAvaloniaViewModel>();
                services.AddTransient<AmazonEuAvaloniaViewModel>();
                services.AddTransient<BarcodeAvaloniaViewModel>();
                services.AddTransient<CalendarAvaloniaViewModel>();
                services.AddTransient<CargoSettingsAvaloniaViewModel>();
                services.AddTransient<CiceksepetiAvaloniaViewModel>();
                services.AddTransient<ContactAvaloniaViewModel>();
                services.AddTransient<CrmDashboardAvaloniaViewModel>();
                services.AddTransient<PlatformMessagesAvaloniaViewModel>();
                services.AddTransient<CrmSettingsAvaloniaViewModel>();
                services.AddTransient<DealsAvaloniaViewModel>();
                services.AddTransient<DepartmentAvaloniaViewModel>();
                services.AddTransient<DocumentFolderAvaloniaViewModel>();
                services.AddTransient<DocumentManagerAvaloniaViewModel>();
                services.AddTransient<EbayAvaloniaViewModel>();
                services.AddTransient<ErpSettingsAvaloniaViewModel>();
                services.AddTransient<GLTransactionAvaloniaViewModel>();
                services.AddTransient<GelirGiderAvaloniaViewModel>();
                services.AddTransient<HealthAvaloniaViewModel>();
                services.AddTransient<HepsiburadaAvaloniaViewModel>();
                services.AddTransient<InvoiceSettingsAvaloniaViewModel>();
                services.AddTransient<KanbanBoardAvaloniaViewModel>();
                services.AddTransient<KarZararAvaloniaViewModel>();
                services.AddTransient<MesaAvaloniaViewModel>();
                services.AddTransient<MultiTenantAvaloniaViewModel>();
                services.AddTransient<MutabakatAvaloniaViewModel>();
                services.AddTransient<N11AvaloniaViewModel>();
                services.AddTransient<NotificationAvaloniaViewModel>();
                services.AddTransient<OpenCartAvaloniaViewModel>();
                services.AddTransient<OrderDetailAvaloniaViewModel>();
                services.AddTransient<OrderListAvaloniaViewModel>();
                services.AddTransient<OzonAvaloniaViewModel>();
                services.AddTransient<PazaramaAvaloniaViewModel>();
                services.AddTransient<PipelineAvaloniaViewModel>();
                services.AddTransient<ProfitLossAvaloniaViewModel>();
                services.AddTransient<ProjectsAvaloniaViewModel>();
                services.AddTransient<PttAvmAvaloniaViewModel>();
                services.AddTransient<ReportAvaloniaViewModel>();
                services.AddTransient<ShipmentAvaloniaViewModel>();
                services.AddTransient<StoreManagementAvaloniaViewModel>();
                services.AddTransient<SupplierAvaloniaViewModel>();
                services.AddTransient<TenantAvaloniaViewModel>();
                services.AddTransient<TimeEntryAvaloniaViewModel>();
                services.AddTransient<TrendyolAvaloniaViewModel>();
                services.AddTransient<UserManagementAvaloniaViewModel>();
                services.AddTransient<WarehouseAvaloniaViewModel>();
                // EMR-06 Gorev 4D: Stok Yerlesim + Lot + Transfer
                services.AddTransient<StockPlacementAvaloniaViewModel>();
                services.AddTransient<StockLotAvaloniaViewModel>();
                services.AddTransient<StockTransferAvaloniaViewModel>();
                // Invoice views (e-Fatura batch)
                services.AddTransient<InvoiceListAvaloniaViewModel>();
                services.AddTransient<InvoiceCreateAvaloniaViewModel>();
                services.AddTransient<BulkInvoiceAvaloniaViewModel>();
                services.AddTransient<InvoicePdfAvaloniaViewModel>();
                services.AddTransient<InvoiceProviderSettingsAvaloniaViewModel>();
                services.AddTransient<InvoiceReportAvaloniaViewModel>();
                services.AddTransient<WelcomeAvaloniaViewModel>();
                services.AddTransient<WorkScheduleAvaloniaViewModel>();
                services.AddTransient<WorkTaskAvaloniaViewModel>();
                // EMR-10 Platform + Dropshipping ViewModels
                services.AddTransient<PlatformListAvaloniaViewModel>();
                services.AddTransient<StoreWizardAvaloniaViewModel>();
                services.AddTransient<CategoryMappingAvaloniaViewModel>();
                services.AddTransient<DropshipDashboardAvaloniaViewModel>();
                services.AddTransient<FeedPreviewAvaloniaViewModel>();
                services.AddTransient<StoreSettingsAvaloniaViewModel>();
                services.AddTransient<StoreDetailAvaloniaViewModel>();
                services.AddTransient<ProductFetchAvaloniaViewModel>();
                services.AddTransient<SupplierFeedListAvaloniaViewModel>();
                services.AddTransient<FeedCreateAvaloniaViewModel>();
                services.AddTransient<DropshipOrdersAvaloniaViewModel>();
                services.AddTransient<DropshipProfitAvaloniaViewModel>();
                services.AddTransient<ImportSettingsAvaloniaViewModel>();
                services.AddTransient<PlatformSyncStatusAvaloniaViewModel>();
            })
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // EMR-02: Baslangic → WelcomeWindow (ekran koruyucu)
            // WelcomeWindow → LoginWindow → MainWindow (DI) akisi
            desktop.MainWindow = new WelcomeWindow();
            desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnLastWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
