using MesTech.Domain.Common;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Dalga 5 D-07: Tum is verisi entity'lerinin ITenantEntity implement ettigini dogrular.
/// </summary>
public class MultiTenantAuditTests
{
    [Theory]
    [InlineData(typeof(OfflineQueueItem))]
    [InlineData(typeof(SyncRetryItem))]
    [InlineData(typeof(StoreCredential))]
    [InlineData(typeof(ProductPlatformMapping))]
    [InlineData(typeof(BrandPlatformMapping))]
    [InlineData(typeof(CategoryPlatformMapping))]
    [InlineData(typeof(InvoiceLine))]
    [InlineData(typeof(ReturnRequestLine))]
    [InlineData(typeof(InvoiceTemplate))]
    [InlineData(typeof(KontorBalance))]
    public void Entity_Should_Implement_ITenantEntity(Type entityType)
    {
        Assert.True(
            typeof(ITenantEntity).IsAssignableFrom(entityType),
            $"{entityType.Name} must implement ITenantEntity for multi-tenant query filter");
    }

    [Theory]
    [InlineData(typeof(OfflineQueueItem))]
    [InlineData(typeof(SyncRetryItem))]
    [InlineData(typeof(StoreCredential))]
    [InlineData(typeof(ProductPlatformMapping))]
    [InlineData(typeof(BrandPlatformMapping))]
    [InlineData(typeof(CategoryPlatformMapping))]
    [InlineData(typeof(InvoiceLine))]
    [InlineData(typeof(ReturnRequestLine))]
    public void Entity_Should_Have_TenantId_Property(Type entityType)
    {
        var prop = entityType.GetProperty("TenantId");
        Assert.NotNull(prop);
        Assert.Equal(typeof(Guid), prop!.PropertyType);
    }
}
