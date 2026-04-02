using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MesTech.Application.Interfaces;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using MesTech.Tests.Headless.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Tests.Headless;

/// <summary>
/// Katman 1.5 — GERÇEK DI + PostgreSQL + Seed Data ile BulkScreenshot.
/// Katman 1: View render oluyor mu? → %100 evet.
/// Katman 1.5: View VERİ gösteriyor mu? → Bu test ile ölçülür.
/// </summary>
[Collection("HeadlessPostgresCollection")]
[Trait("Category", "Headless")]
[Trait("Layer", "UI")]
public class BulkScreenshotLayer15Tests
{
    private readonly TestPostgresFactory _pgFactory;
    private readonly IServiceProvider _serviceProvider;

    public BulkScreenshotLayer15Tests(TestPostgresFactory pgFactory)
    {
        _pgFactory = pgFactory;
        _serviceProvider = BuildDiContainer();
        SeedTestData().GetAwaiter().GetResult();
    }

    private async Task SeedTestData()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Tenant + Warehouse + Products + Orders seed — Katman 1.5 "22 LOADING → <5" hedefi
        if (await context.Tenants.AnyAsync()) return; // idempotent

        var tenantId = DataSeeder.DefaultTenantId;
        var tenant = new Domain.Entities.Tenant { Name = "Test Tenant", IsActive = true };
        typeof(Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(tenant, tenantId);
        context.Tenants.Add(tenant);

        context.Warehouses.Add(new Domain.Entities.Warehouse { Name = "Ana Depo", Code = "WH-01", TenantId = tenantId, IsActive = true });
        context.Warehouses.Add(new Domain.Entities.Warehouse { Name = "Yedek Depo", Code = "WH-02", TenantId = tenantId, IsActive = true });

        for (int i = 1; i <= 10; i++)
        {
            context.Products.Add(new Domain.Entities.Product
            {
                SKU = $"TST-{i:D4}",
                Name = $"Test Urun {i}",
                Barcode = $"869000{i:D6}",
                SalePrice = 100m * i,
                PurchasePrice = 60m * i,
                Stock = 50 - i * 3,
                MinimumStock = 5,
                TenantId = tenantId,
                CreatedBy = "seed",
                UpdatedBy = "seed"
            });
        }

        for (int i = 1; i <= 5; i++)
        {
            var order = Domain.Entities.Order.CreateFromPlatform(
                tenantId, $"PLT-ORD-{i:D4}",
                Domain.Enums.PlatformType.Trendyol,
                $"Musteri {i}", $"musteri{i}@test.com",
                Array.Empty<Domain.Entities.OrderItem>());
            context.Orders.Add(order);
        }

        context.Categories.Add(new Domain.Entities.Category { Name = "Elektronik", Code = "ELEC", TenantId = tenantId, IsActive = true });
        context.Categories.Add(new Domain.Entities.Category { Name = "Giyim", Code = "WEAR", TenantId = tenantId, IsActive = true });

        await context.SaveChangesAsync();
    }

    private IServiceProvider BuildDiContainer()
    {
        var services = new ServiceCollection();

        // Configuration — in-memory
        var configValues = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = _pgFactory.ConnectionString,
            ["WebApi:BaseUrl"] = "http://localhost:3100"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Logging
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Tenant & User — Headless test sabit GUID
        services.AddSingleton<ITenantProvider>(new HeadlessTestTenantProvider());
        services.AddSingleton<ICurrentUserService, HeadlessTestUserService>();

        // Avalonia UI stub servisleri — headless'ta pencere/dialog yok
        services.AddSingleton<MesTech.Avalonia.Services.IDialogService, Infrastructure.HeadlessDialogService>();
        services.AddSingleton<MesTech.Avalonia.Services.IThemeService, Infrastructure.HeadlessThemeService>();
        services.AddSingleton<MesTech.Avalonia.Services.IFilePickerService, Infrastructure.HeadlessFilePickerService>();
        services.AddSingleton<MesTech.Avalonia.Services.INavigationService, Infrastructure.HeadlessNavigationService>();
        services.AddSingleton<MesTech.Avalonia.Services.IFeatureGateService, Infrastructure.HeadlessFeatureGateService>();
        services.AddSingleton<MesTech.Avalonia.Services.INotificationService, Infrastructure.HeadlessNotificationService>();

        // EF Core — TestPostgresFactory connection string ile
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(_pgFactory.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
            options.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(global::MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly));

        // Repositories — InfrastructureServiceRegistration'dan
        RegisterRepositories(services);

        // Platform adapters — InMemory (gerçek API çağrısı yok)
        services.AddSingleton<IAdapterFactory>(new InMemoryAdapterFactory());

        // ViewModels — MesTech.Avalonia assembly'den tüm ViewModel'leri otomatik kaydet
        RegisterAllViewModels(services);

        // IViewModelFactory — ServiceProvider gerektirir, son kayıt
        services.AddSingleton<MesTech.Avalonia.Services.IViewModelFactory>(sp =>
            new Infrastructure.HeadlessViewModelFactory(sp));

        return services.BuildServiceProvider();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        var infraAssembly = typeof(AppDbContext).Assembly;

        // Repository interface → implementation otomatik eşleme
        var repoTypes = infraAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Repository"))
            .ToList();

        foreach (var implType in repoTypes)
        {
            var interfaces = implType.GetInterfaces()
                .Where(i => i.Name.EndsWith("Repository") && i != typeof(IDisposable))
                .ToList();

            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, implType);
            }
        }

        // UnitOfWork
        var uowImpl = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "UnitOfWork" && !t.IsAbstract);
        if (uowImpl != null)
        {
            var uowInterface = uowImpl.GetInterfaces().FirstOrDefault(i => i.Name == "IUnitOfWork");
            if (uowInterface != null)
                services.AddScoped(uowInterface, uowImpl);
        }
    }

    private static void RegisterAllViewModels(IServiceCollection services)
    {
        var avaloniaAssembly = typeof(MesTech.Avalonia.App).Assembly;
        var vmTypes = avaloniaAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .Where(t => typeof(ViewModelBase).IsAssignableFrom(t))
            .ToList();

        foreach (var vmType in vmTypes)
        {
            services.AddTransient(vmType);
        }
    }

    [AvaloniaFact]
    public void CaptureAllViewsWithDI()
    {
        var outputDir = "screenshots/katman1.5";
        Directory.CreateDirectory(outputDir);

        var avaloniaAssembly = typeof(MesTech.Avalonia.App).Assembly;
        var viewTypes = avaloniaAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("View") || t.Name.EndsWith("Window"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(Control).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(t => t.Name)
            .ToList();

        int captured = 0;
        int failed = 0;
        int withData = 0;    // >20KB = muhtemelen veri gosteriyor
        int loading = 0;     // 5-10KB = loading/error state
        int minimal = 0;     // 10-20KB = minimal icerik
        var errors = new List<string>();
        var sizeReport = new List<(string Name, long Size, string Category)>();

        foreach (var viewType in viewTypes)
        {
            try
            {
                var view = (Control)Activator.CreateInstance(viewType)!;

                // ViewModel resolve edip DataContext olarak ata
                var vmTypeName = viewType.Name.Replace("View", "ViewModel");
                if (viewType.Name.EndsWith("Window"))
                    vmTypeName = viewType.Name.Replace("Window", "ViewModel");

                var vmType = typeof(MesTech.Avalonia.App).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == vmTypeName && typeof(ViewModelBase).IsAssignableFrom(t));

                if (vmType != null)
                {
                    try
                    {
                        var vm = _serviceProvider.GetService(vmType);
                        if (vm != null)
                            view.DataContext = vm;
                    }
                    catch { /* ViewModel DI resolve hata — DataContext null kalir */ }
                }

                Window window;
                if (view is Window w)
                {
                    window = w;
                    window.Width = 1280;
                    window.Height = 720;
                }
                else
                {
                    window = new Window
                    {
                        Width = 1280,
                        Height = 720,
                        Content = view
                    };
                }

                window.Show();
                Dispatcher.UIThread.RunJobs();

                // InitializeAsync tetiklenmesi icin ekstra bekleme
                Thread.Sleep(200);
                Dispatcher.UIThread.RunJobs();

                var frame = window.CaptureRenderedFrame();
                if (frame != null)
                {
                    var filename = Path.Combine(outputDir, $"{viewType.Name}.png");
                    frame.Save(filename);

                    var fileSize = new FileInfo(filename).Length;
                    string category;
                    if (fileSize > 20_000) { withData++; category = "DATA"; }
                    else if (fileSize > 10_000) { minimal++; category = "MINIMAL"; }
                    else { loading++; category = "LOADING"; }

                    sizeReport.Add((viewType.Name, fileSize, category));
                    captured++;
                }
                else
                {
                    errors.Add($"{viewType.Name}: CaptureRenderedFrame NULL");
                    failed++;
                }

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null
                    ? $"\n\nINNER: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                    : "";
                File.WriteAllText(
                    Path.Combine(outputDir, $"{viewType.Name}_ERROR.txt"),
                    $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}{innerMsg}");
                errors.Add($"{viewType.Name}: {ex.GetType().Name} — {ex.InnerException?.Message ?? ex.Message}");
                failed++;
            }
        }

        // KATMAN 1.5 RAPOR
        var report = $"# KATMAN 1.5 RAPOR — DI + PostgreSQL + Seed Data\n" +
                     $"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                     $"## OZET\n" +
                     $"Toplam View: {viewTypes.Count}\n" +
                     $"Basarili Screenshot: {captured}\n" +
                     $"Basarisiz: {failed}\n\n" +
                     $"## KATEGORI DAGILIMI\n" +
                     $"DATA (>20KB — veri gosteriyor): {withData}\n" +
                     $"MINIMAL (10-20KB — az icerik): {minimal}\n" +
                     $"LOADING (5-10KB — spinner/hata): {loading}\n\n";

        // En buyuk 20 view (muhtemelen en cok veri gösteren)
        report += "## EN BUYUK 20 VIEW (en cok veri)\n";
        foreach (var item in sizeReport.OrderByDescending(s => s.Size).Take(20))
            report += $"  {item.Size / 1024,4}KB  [{item.Category}]  {item.Name}\n";

        // En kucuk 20 view (muhtemelen bos/loading)
        report += "\n## EN KUCUK 20 VIEW (potansiyel bos)\n";
        foreach (var item in sizeReport.OrderBy(s => s.Size).Take(20))
            report += $"  {item.Size / 1024,4}KB  [{item.Category}]  {item.Name}\n";

        if (errors.Count > 0)
        {
            report += $"\n## HATALAR ({errors.Count})\n";
            foreach (var err in errors)
                report += $"  - {err}\n";
        }

        File.WriteAllText($"{outputDir}/KATMAN15_RAPOR.md", report);
        File.WriteAllText("screenshots/RAPOR_KATMAN15.txt",
            $"Katman 1.5: {captured}/{viewTypes.Count} screenshot, DATA:{withData} MINIMAL:{minimal} LOADING:{loading} FAIL:{failed}");

        Assert.True(captured > 0, $"Hicbir view screenshot alinamadi! {failed} hata.");
    }
}

/// <summary>
/// Headless test için sabit kullanıcı servisi.
/// </summary>
public sealed class HeadlessTestUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000099");
    public string? Username => "headless_admin";
    public Guid TenantId => TestSeedDataFactory.TestTenantId;
    public bool IsAuthenticated => true;
}
