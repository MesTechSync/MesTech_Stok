using MesTech.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// IServiceLocatorBridge implementasyonu.
/// WPF Converter ve code-behind için izole DI erişimi sağlar.
/// App.xaml.cs dışında doğrudan kullanım yasak.
/// ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-01
/// </summary>
public sealed class ServiceLocatorBridge(IServiceProvider serviceProvider) : IServiceLocatorBridge
{
    public T GetRequiredService<T>() where T : notnull
        => serviceProvider.GetRequiredService<T>();

    public T? GetService<T>()
        => serviceProvider.GetService<T>();

    public IServiceScope CreateScope()
        => serviceProvider.CreateScope();
}
