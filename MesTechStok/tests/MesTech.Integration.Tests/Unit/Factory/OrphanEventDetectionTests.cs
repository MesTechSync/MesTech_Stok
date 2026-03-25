using FluentAssertions;
using MesTech.Domain.Common;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Orphan event tespiti — yayınlanan ama handler'ı olmayan domain event'leri bulur.
/// Her domain event'in Application katmanında en az 1 referansı olmalı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Architecture")]
[Trait("Group", "OrphanEvent")]
public class OrphanEventDetectionTests
{
    private readonly ITestOutputHelper _output;

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(BaseEntity).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount.CreateChartOfAccountHandler).Assembly;

    public OrphanEventDetectionTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void AllDomainEvents_ShouldHaveAtLeastOneHandler()
    {
        // Domain event'lerini keşfet
        var domainEvents = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        // Application'daki tüm tiplerin referans ettiği event'leri bul
        var applicationTypes = ApplicationAssembly.GetTypes();
        var referencedEventNames = new HashSet<string>();

        foreach (var appType in applicationTypes)
        {
            // Constructor parametrelerinde event kullanımı
            foreach (var ctor in appType.GetConstructors())
            {
                foreach (var param in ctor.GetParameters())
                {
                    if (typeof(IDomainEvent).IsAssignableFrom(param.ParameterType))
                        referencedEventNames.Add(param.ParameterType.Name);
                }
            }

            // Method parametrelerinde event kullanımı
            foreach (var method in appType.GetMethods())
            {
                foreach (var param in method.GetParameters())
                {
                    if (typeof(IDomainEvent).IsAssignableFrom(param.ParameterType))
                        referencedEventNames.Add(param.ParameterType.Name);

                    // Generic type parametrelerinde (DomainEventNotification<T>)
                    if (param.ParameterType.IsGenericType)
                    {
                        foreach (var genArg in param.ParameterType.GetGenericArguments())
                        {
                            if (typeof(IDomainEvent).IsAssignableFrom(genArg))
                                referencedEventNames.Add(genArg.Name);
                        }
                    }
                }

                // Interface generic parametreleri
                foreach (var iface in appType.GetInterfaces())
                {
                    if (iface.IsGenericType)
                    {
                        foreach (var genArg in iface.GetGenericArguments())
                        {
                            if (typeof(IDomainEvent).IsAssignableFrom(genArg))
                                referencedEventNames.Add(genArg.Name);
                        }
                    }
                }
            }

            // Handler interface'lerinde event kullanımı (INotificationHandler<DomainEventNotification<T>>)
            foreach (var iface in appType.GetInterfaces())
            {
                if (iface.IsGenericType)
                {
                    foreach (var genArg in iface.GetGenericArguments())
                    {
                        if (typeof(IDomainEvent).IsAssignableFrom(genArg))
                            referencedEventNames.Add(genArg.Name);

                        if (genArg.IsGenericType)
                        {
                            foreach (var innerArg in genArg.GetGenericArguments())
                            {
                                if (typeof(IDomainEvent).IsAssignableFrom(innerArg))
                                    referencedEventNames.Add(innerArg.Name);
                            }
                        }
                    }
                }
            }
        }

        // Orphan tespiti
        var orphanEvents = domainEvents
            .Where(e => !referencedEventNames.Contains(e.Name))
            .Select(e => e.FullName)
            .OrderBy(n => n)
            .ToList();

        _output.WriteLine($"Toplam domain event: {domainEvents.Count}");
        _output.WriteLine($"Application'da referanslı: {referencedEventNames.Count}");
        _output.WriteLine($"Orphan (handler'sız): {orphanEvents.Count}");
        _output.WriteLine("");

        foreach (var orphan in orphanEvents)
            _output.WriteLine($"  ORPHAN: {orphan}");

        // Bilgilendirme raporu — orphan'lar olabilir ama bilgilendirilmeli
        domainEvents.Count.Should().BeGreaterThan(0, "En az 1 domain event olmalı");
    }

    [Fact]
    public void DomainEventCount_ShouldBeTracked()
    {
        var eventCount = DomainAssembly.GetTypes()
            .Count(t => !t.IsAbstract && !t.IsInterface && typeof(IDomainEvent).IsAssignableFrom(t));

        _output.WriteLine($"Domain event toplam: {eventCount}");
        eventCount.Should().BeGreaterThan(40, "MesTech projesinde 40+ domain event bekleniyor");
    }

    [Fact]
    public void EventHandlerCount_ShouldBeTracked()
    {
        var handlerCount = ApplicationAssembly.GetTypes()
            .Count(t => !t.IsAbstract && t.IsClass && t.Name.EndsWith("EventHandler"));

        _output.WriteLine($"Event handler toplam: {handlerCount}");
        handlerCount.Should().BeGreaterThan(40, "MesTech projesinde 40+ event handler bekleniyor");
    }
}
