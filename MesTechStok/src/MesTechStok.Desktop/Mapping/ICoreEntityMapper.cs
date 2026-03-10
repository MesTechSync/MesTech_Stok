using CoreProduct = MesTechStok.Core.Data.Models.Product;
using CoreCategory = MesTechStok.Core.Data.Models.Category;
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
