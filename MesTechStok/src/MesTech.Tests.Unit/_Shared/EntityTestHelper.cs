using MesTech.Domain.Common;

namespace MesTech.Tests.Unit._Shared;

/// <summary>
/// Test helper for setting entity Id via reflection.
/// BaseEntity.Id has no public setter — tests that need a known Id use this helper.
/// Centralised here so refactoring BaseEntity only requires one change.
/// </summary>
public static class EntityTestHelper
{
    /// <summary>
    /// Sets the Id property on any <see cref="BaseEntity"/> subclass using reflection.
    /// </summary>
    public static void SetEntityId<T>(T entity, Guid id) where T : BaseEntity
    {
        typeof(T).BaseType!
            .GetProperty("Id")!
            .SetValue(entity, id);
    }
}
