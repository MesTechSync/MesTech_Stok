#pragma warning disable CS0618 // Core.Data.Models type aliases — mapper bridge, will be removed in H32
using CoreProduct = MesTechStok.Core.Data.Models.Product;
using CoreCategory = MesTechStok.Core.Data.Models.Category;
#pragma warning restore CS0618
using DomainProduct = MesTech.Domain.Entities.Product;
using DomainCategory = MesTech.Domain.Entities.Category;

namespace MesTechStok.Desktop.Mapping;

/// <summary>
/// Core ↔ Domain entity mapper — gradual migration bridge.
/// Core entities (int PK, MVVM) ↔ Domain entities (Guid PK, DDD).
/// </summary>
public interface ICoreEntityMapper
{
    DomainProduct ToDomainProduct(CoreProduct core);
    CoreProduct ToCoreProduct(DomainProduct domain);
    DomainCategory ToDomainCategory(CoreCategory core);
    CoreCategory ToCoreCategory(DomainCategory domain);
}
