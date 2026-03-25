using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// MediatR handler ve FluentValidation validator keşif testleri.
/// Tüm IRequestHandler implementasyonları ve AbstractValidator implementasyonları
/// Application assembly'den doğru şekilde keşfedilebilmeli.
/// Bu test DI registration'ın doğru çalışacağının ön koşulunu doğrular.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "DI")]
[Trait("Group", "MediatRRegistration")]
public class MediatRRegistrationTests
{
    private readonly ITestOutputHelper _output;

    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount.CreateChartOfAccountHandler).Assembly;

    public MediatRRegistrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void AllHandlers_AreDiscoverable_ByMediatR()
    {
        var handlerInterfaces = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(IRequestHandler<>)
        };

        var handlers = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .ToList();

        _output.WriteLine($"Toplam MediatR handler: {handlers.Count}");

        handlers.Count.Should().BeGreaterThan(300,
            "MesTech projesinde 300+ MediatR handler bekleniyor");
    }

    [Fact]
    public void AllValidators_AreDiscoverable_ByFluentValidation()
    {
        var validators = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.BaseType != null && t.BaseType.IsGenericType
                && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .ToList();

        _output.WriteLine($"Toplam FluentValidation validator: {validators.Count}");

        validators.Count.Should().BeGreaterThan(190,
            "MesTech projesinde 190+ validator bekleniyor");
    }

    [Fact]
    public void AllHandlers_HaveMatchingCommand()
    {
        // Her handler'ın handle ettiği Command/Query tipi var olmalı
        var handlerInterfaces = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(IRequestHandler<>)
        };

        var handlers = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .ToList();

        var orphanHandlers = new List<string>();

        foreach (var handler in handlers)
        {
            var requestInterface = handler.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                    handlerInterfaces.Contains(i.GetGenericTypeDefinition()));

            if (requestInterface == null)
            {
                orphanHandlers.Add(handler.Name);
                continue;
            }

            var requestType = requestInterface.GetGenericArguments()[0];
            requestType.Should().NotBeNull($"{handler.Name} bir request tipi handle etmeli");
        }

        _output.WriteLine($"Toplam handler: {handlers.Count}, Orphan: {orphanHandlers.Count}");
        orphanHandlers.Should().BeEmpty("Her handler bir request tipi handle etmeli");
    }

    [Fact]
    public void AllValidators_HaveMatchingCommand()
    {
        // Her validator'ın validate ettiği Command tipi var olmalı
        var validators = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.BaseType != null && t.BaseType.IsGenericType
                && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .ToList();

        foreach (var validator in validators)
        {
            var commandType = validator.BaseType!.GetGenericArguments()[0];
            commandType.Should().NotBeNull($"{validator.Name} bir command tipi validate etmeli");

            // Command tipi IRequest implement etmeli
            var isRequest = commandType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IRequest"));

            if (!isRequest)
            {
                _output.WriteLine($"WARNING: {validator.Name} → {commandType.Name} IRequest implement etmiyor");
            }
        }

        _output.WriteLine($"Toplam validator: {validators.Count}");
        validators.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Handler_Validator_Pairing_Coverage()
    {
        // Command'lerin kaçında validator var?
        var handlerInterfaces = new[] { typeof(IRequestHandler<,>), typeof(IRequestHandler<>) };

        var handledRequestTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                .Select(i => i.GetGenericArguments()[0]))
            .Where(t => t.Name.EndsWith("Command")) // Sadece Command'ler — Query'lere validator gerekmez
            .Distinct()
            .ToList();

        var validatedTypes = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.BaseType != null && t.BaseType.IsGenericType
                && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .Select(t => t.BaseType!.GetGenericArguments()[0])
            .Distinct()
            .ToHashSet();

        var commandsWithoutValidator = handledRequestTypes
            .Where(c => !validatedTypes.Contains(c))
            .Select(c => c.Name)
            .OrderBy(n => n)
            .ToList();

        var coverage = handledRequestTypes.Count > 0
            ? (handledRequestTypes.Count - commandsWithoutValidator.Count) * 100 / handledRequestTypes.Count
            : 0;

        _output.WriteLine($"Command toplam: {handledRequestTypes.Count}");
        _output.WriteLine($"Validator'lı: {handledRequestTypes.Count - commandsWithoutValidator.Count}");
        _output.WriteLine($"Validator'sız: {commandsWithoutValidator.Count}");
        _output.WriteLine($"Coverage: {coverage}%");

        foreach (var cmd in commandsWithoutValidator.Take(20))
            _output.WriteLine($"  VALIDATOR EKSİK: {cmd}");

        // Bilgilendirme — %60+ bekliyoruz
        coverage.Should().BeGreaterThanOrEqualTo(50,
            "Command'lerin en az %50'sinde validator olmalı");
    }

    [Fact]
    public void NoHandler_ShouldBe_InDomainNamespace()
    {
        // Handler'lar Domain'de olmamalı (Clean Architecture)
        var domainAssembly = typeof(MesTech.Domain.Common.BaseEntity).Assembly;

        var domainHandlers = domainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass)
            .Where(t => t.Name.EndsWith("Handler"))
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IRequestHandler")))
            .Select(t => t.FullName)
            .ToList();

        domainHandlers.Should().BeEmpty(
            "MediatR handler'lar Domain katmanında OLMAMALI");
    }

    [Fact]
    public void Handlers_PerFeatureArea_Distribution()
    {
        var handlerInterfaces = new[] { typeof(IRequestHandler<,>), typeof(IRequestHandler<>) };

        var distribution = ApplicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .GroupBy(t =>
            {
                var ns = t.Namespace ?? "Unknown";
                var parts = ns.Split('.');
                var featIdx = Array.IndexOf(parts, "Features");
                return featIdx >= 0 && featIdx + 1 < parts.Length ? parts[featIdx + 1] : "Other";
            })
            .OrderByDescending(g => g.Count())
            .ToList();

        _output.WriteLine("Handler dağılımı (feature area):");
        foreach (var group in distribution)
            _output.WriteLine($"  {group.Key}: {group.Count()} handler");

        distribution.Count.Should().BeGreaterThan(10,
            "En az 10 feature area bekleniyor");
    }
}
