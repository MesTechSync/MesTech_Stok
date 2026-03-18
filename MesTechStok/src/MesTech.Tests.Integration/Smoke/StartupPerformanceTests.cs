using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Tests.Integration.Smoke;

/// <summary>
/// Emirname G4: Performance baseline — startup time and memory usage.
/// S13: Startup &lt; 5 seconds
/// S14: Memory &lt; 300 MB
/// </summary>
[Trait("Category", "Performance")]
[Trait("Category", "Smoke")]
public class StartupPerformanceTests
{
    [Fact(Skip = "Requires physical display and compiled WPF EXE — run locally only")]
    public void Startup_ShouldCompleteWithin5Seconds()
    {
        var exePath = UI._Shared.DesktopAppFixture.FindDesktopExe();
        if (exePath == null) return; // Skip if EXE not built

        var sw = Stopwatch.StartNew();
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
        });

        process.Should().NotBeNull("Desktop app should launch");

        var timeout = TimeSpan.FromSeconds(30);
        while (sw.Elapsed < timeout && process!.MainWindowHandle == IntPtr.Zero)
        {
            Thread.Sleep(200);
            process.Refresh();
        }

        sw.Stop();

        // Emirname S13: Startup < 5 seconds
        sw.ElapsedMilliseconds.Should().BeLessThan(5000,
            $"Startup took {sw.ElapsedMilliseconds}ms, Emirname limit is 5000ms (S13)");

        try { process!.Kill(); process.WaitForExit(5000); } catch { /* Intentional: test cleanup — process disposal failure non-critical */ }
        try { process!.Dispose(); } catch { /* Intentional: test cleanup — process disposal failure non-critical */ }
    }

    [Fact(Skip = "Requires physical display and compiled WPF EXE — run locally only")]
    public void Memory_ShouldBeUnder300MB()
    {
        var exePath = UI._Shared.DesktopAppFixture.FindDesktopExe();
        if (exePath == null) return;

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
        });

        process.Should().NotBeNull();

        var timeout = TimeSpan.FromSeconds(30);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout && process!.MainWindowHandle == IntPtr.Zero)
        {
            Thread.Sleep(200);
            process.Refresh();
        }

        Thread.Sleep(3000); // Stabilize after startup
        process!.Refresh();

        var workingSetMB = process.WorkingSet64 / (1024.0 * 1024.0);

        // Emirname S14: Memory < 300 MB
        workingSetMB.Should().BeLessThan(300,
            $"Working set is {workingSetMB:F1}MB, Emirname limit is 300MB (S14)");

        try { process.Kill(); process.WaitForExit(5000); } catch { /* Intentional: test cleanup — process disposal failure non-critical */ }
        try { process.Dispose(); } catch { /* Intentional: test cleanup — process disposal failure non-critical */ }
    }

    /// <summary>
    /// DI container build time baseline (headless — works in CI).
    /// </summary>
    [Fact]
    public void DiContainerBuild_ShouldCompleteUnder2Seconds()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            ["Database:Provider"] = "PostgreSQL",
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        services.AddDbContext<MesTechStok.Core.Data.AppDbContext>(options =>
            options.UseInMemoryDatabase($"PerfTest_{Guid.NewGuid()}"));

        services.AddScoped<MesTech.Infrastructure.Persistence.AuditInterceptor>();
        services.AddDbContext<MesTech.Infrastructure.Persistence.AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase($"PerfInfra_{Guid.NewGuid()}");
            options.AddInterceptors(sp.GetRequiredService<MesTech.Infrastructure.Persistence.AuditInterceptor>());
        });

        services.AddSingleton<MesTech.Domain.Interfaces.ITenantProvider, MesTech.Infrastructure.Security.DevelopmentTenantProvider>();
        services.AddSingleton<MesTech.Domain.Interfaces.ICurrentUserService, MesTech.Infrastructure.Security.DevelopmentUserService>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MesTech.Application.Commands.AddStock.AddStockHandler).Assembly));

        var sw = Stopwatch.StartNew();
        using var provider = services.BuildServiceProvider();
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(2000,
            $"DI build took {sw.ElapsedMilliseconds}ms, limit is 2000ms");
    }
}
