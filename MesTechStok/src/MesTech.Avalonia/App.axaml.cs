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
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia;

/// <summary>
/// Avalonia App entry point with DI — mirrors WPF App.xaml.cs pattern.
/// Uses SAME AddInfrastructure() registration as WPF Desktop.
/// Domain + Application + Infrastructure = ZERO CHANGES.
/// </summary>
public partial class App : global::Avalonia.Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = ConfigureServices();
        Services = services;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            // Auto-navigate to Dashboard on startup
            mainVm.NavigateToCommand.Execute("Dashboard");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IConfiguration>(configuration);

        // === SAME Infrastructure DI as WPF Desktop — ZERO CHANGES ===
        services.AddInfrastructure(configuration);

        // MediatR — Application CQRS handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(global::MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly));

        // === Avalonia-specific services ===
        services.AddSingleton<IDialogService, ConsoleDialogService>();

        // ViewModels — Core (Dalga 10)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardAvaloniaViewModel>();
        services.AddTransient<LeadsAvaloniaViewModel>();
        services.AddTransient<KanbanAvaloniaViewModel>();
        services.AddTransient<ProductsAvaloniaViewModel>();
        services.AddTransient<StockAvaloniaViewModel>();
        services.AddTransient<OrdersAvaloniaViewModel>();
        services.AddTransient<SettingsAvaloniaViewModel>();
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

        return services.BuildServiceProvider();
    }
}
