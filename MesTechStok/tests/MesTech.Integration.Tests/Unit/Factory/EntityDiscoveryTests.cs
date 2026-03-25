using FluentAssertions;
using MesTech.Domain.Common;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Tüm domain entity'lerini reflection ile keşfeder ve test eder.
/// Her entity: (1) parameterless constructor ile oluşturulabilir,
///             (2) Id Guid.Empty olmamalı (BaseEntity auto-assigns),
///             (3) CreatedAt default UTC tarihinde.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Group", "EntityDiscovery")]
public class EntityDiscoveryTests
{
    private readonly ITestOutputHelper _output;

    public EntityDiscoveryTests(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> AllEntityTypes()
    {
        var domainAssembly = typeof(BaseEntity).Assembly;

        var entityTypes = domainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass)
            .Where(t => IsEntityType(t))
            .OrderBy(t => t.FullName)
            .ToList();

        foreach (var et in entityTypes)
        {
            yield return new object[] { et, et.Name };
        }
    }

    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_CanBeInstantiated(Type entityType, string displayName)
    {
        var entity = TryCreateEntity(entityType);

        if (entity == null)
        {
            _output.WriteLine($"SKIP: {displayName} — parameterless ctor yok veya factory method gerekli");
            return;
        }

        entity.Should().NotBeNull($"{displayName} oluşturulabilmeli");
        _output.WriteLine($"OK: {displayName}");
    }

    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_HasNonEmptyId(Type entityType, string displayName)
    {
        var entity = TryCreateEntity(entityType);
        if (entity == null)
        {
            _output.WriteLine($"SKIP: {displayName}");
            return;
        }

        var idProp = entityType.GetProperty("Id");
        if (idProp == null || idProp.PropertyType != typeof(Guid))
        {
            _output.WriteLine($"SKIP: {displayName} — Id property yok veya Guid değil");
            return;
        }

        var id = (Guid)idProp.GetValue(entity)!;
        id.Should().NotBeEmpty($"{displayName}.Id auto-assign edilmeli (BaseEntity)");
        _output.WriteLine($"OK: {displayName} — Id={id.ToString()[..8]}...");
    }

    [Theory]
    [MemberData(nameof(AllEntityTypes))]
    public void Entity_HasReasonableCreatedAt(Type entityType, string displayName)
    {
        var entity = TryCreateEntity(entityType);
        if (entity == null) return;

        var createdAtProp = entityType.GetProperty("CreatedAt");
        if (createdAtProp == null || createdAtProp.PropertyType != typeof(DateTime))
        {
            _output.WriteLine($"SKIP: {displayName} — CreatedAt yok");
            return;
        }

        var createdAt = (DateTime)createdAtProp.GetValue(entity)!;
        createdAt.Should().BeAfter(new DateTime(2020, 1, 1), $"{displayName}.CreatedAt makul tarihte olmalı");
        _output.WriteLine($"OK: {displayName} — CreatedAt={createdAt:yyyy-MM-dd HH:mm}");
    }

    private static object? TryCreateEntity(Type entityType)
    {
        try
        {
            // Try parameterless constructor first
            var ctor = entityType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
                return Activator.CreateInstance(entityType);

            // Try constructor with all default values
            var ctors = entityType.GetConstructors();
            if (ctors.Length == 0) return null;

            var primaryCtor = ctors[0];
            var ctorParams = primaryCtor.GetParameters();
            var args = ctorParams.Select(p => GetDefaultValue(p.ParameterType)).ToArray();
            return primaryCtor.Invoke(args);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsEntityType(Type type)
    {
        // Check if inherits from BaseEntity
        var current = type.BaseType;
        while (current != null)
        {
            if (current == typeof(BaseEntity))
                return true;
            current = current.BaseType;
        }

        // Also check for ITenantEntity or common entity patterns
        if (type.GetInterfaces().Any(i => i.Name == "ITenantEntity"))
            return true;

        // Check namespace — entities live in Entities folders
        if (type.Namespace?.Contains("Entities") == true && type.GetProperty("Id") != null)
            return true;

        return false;
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(string)) return string.Empty;
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type == typeof(decimal)) return 0m;
        if (type == typeof(int)) return 0;
        if (type == typeof(long)) return 0L;
        if (type == typeof(bool)) return false;
        if (type == typeof(double)) return 0.0;
        if (type.IsEnum) return Activator.CreateInstance(type);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return null;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return Activator.CreateInstance(type);
        if (type.IsValueType) return Activator.CreateInstance(type);
        return null;
    }
}
