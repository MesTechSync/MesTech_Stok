#pragma warning disable CS0618 // Core.Data.Models type aliases — mapper bridge, will be removed in H32
using CoreProduct = MesTechStok.Core.Data.Models.Product;
using CoreCategory = MesTechStok.Core.Data.Models.Category;
#pragma warning restore CS0618
using DomainProduct = MesTech.Domain.Entities.Product;
using DomainCategory = MesTech.Domain.Entities.Category;

namespace MesTechStok.Desktop.Mapping;

#pragma warning disable CS0618 // Obsolete members — mapper intentionally bridges Core ↔ Domain
/// <summary>
/// Maps between Core (MVVM, Obsolete) and Domain (DDD) entities.
/// Used during gradual migration — will be removed when Core entities are retired in Dalga 6.
/// </summary>
public class CoreEntityMapper : ICoreEntityMapper
{
    public DomainProduct ToDomainProduct(CoreProduct core)
    {
        ArgumentNullException.ThrowIfNull(core);

        return new DomainProduct
        {
            Name = core.Name ?? string.Empty,
            SKU = core.SKU ?? string.Empty,
            Barcode = core.Barcode,
            Description = core.Description,
            PurchasePrice = core.PurchasePrice,
            SalePrice = core.SalePrice,
            ListPrice = core.ListPrice,
            TaxRate = core.TaxRate,
            DiscountRate = core.DiscountRate,
            Stock = core.Stock,
            MinimumStock = core.MinimumStock,
            MaximumStock = core.MaximumStock,
            ReorderLevel = core.ReorderLevel,
            ReorderQuantity = core.ReorderQuantity,
            Weight = core.Weight,
            Length = core.Length,
            Width = core.Width,
            Height = core.Height,
            DimensionUnit = core.DimensionUnit,
            Desi = core.Desi,
            Location = core.Location,
            Shelf = core.Shelf,
            Bin = core.Bin,
            IsActive = core.IsActive,
            IsDiscontinued = core.IsDiscontinued,
            IsSerialized = core.IsSerialized,
            IsBatchTracked = core.IsBatchTracked,
            IsPerishable = core.IsPerishable,
            ExpiryDate = core.ExpiryDate,
            Brand = core.Brand,
            Model = core.Model,
            Color = core.Color,
            Size = core.Size,
            Sizes = core.Sizes,
            Origin = core.Origin,
            Material = core.Material,
            VolumeText = core.VolumeText,
            LeadTimeDays = core.LeadTimeDays,
            ShipAddress = core.ShipAddress,
            ReturnAddress = core.ReturnAddress,
            UsageInstructions = core.UsageInstructions,
            ImporterInfo = core.ImporterInfo,
            ManufacturerInfo = core.ManufacturerInfo,
            ImageUrl = core.ImageUrl,
            Notes = core.Notes,
            Tags = core.Tags,
            Code = core.Code
        };
    }

    public CoreProduct ToCoreProduct(DomainProduct domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CoreProduct
        {
            Name = domain.Name,
            SKU = domain.SKU,
            Barcode = domain.Barcode ?? string.Empty,
            Description = domain.Description ?? string.Empty,
            PurchasePrice = domain.PurchasePrice,
            SalePrice = domain.SalePrice,
            ListPrice = domain.ListPrice,
            TaxRate = domain.TaxRate,
            DiscountRate = domain.DiscountRate,
            Stock = domain.Stock,
            MinimumStock = domain.MinimumStock,
            MaximumStock = domain.MaximumStock,
            ReorderLevel = domain.ReorderLevel,
            ReorderQuantity = domain.ReorderQuantity,
            Weight = domain.Weight,
            Length = domain.Length,
            Width = domain.Width,
            Height = domain.Height,
            DimensionUnit = domain.DimensionUnit,
            Desi = domain.Desi,
            Location = domain.Location,
            Shelf = domain.Shelf,
            Bin = domain.Bin,
            IsActive = domain.IsActive,
            IsDiscontinued = domain.IsDiscontinued,
            IsSerialized = domain.IsSerialized,
            IsBatchTracked = domain.IsBatchTracked,
            IsPerishable = domain.IsPerishable,
            ExpiryDate = domain.ExpiryDate,
            // LastStockUpdate is private set — managed by domain.UpdateStock()
            Brand = domain.Brand,
            Model = domain.Model,
            Color = domain.Color,
            Size = domain.Size,
            Sizes = domain.Sizes,
            Origin = domain.Origin,
            Material = domain.Material,
            VolumeText = domain.VolumeText,
            LeadTimeDays = domain.LeadTimeDays,
            ShipAddress = domain.ShipAddress,
            ReturnAddress = domain.ReturnAddress,
            UsageInstructions = domain.UsageInstructions,
            ImporterInfo = domain.ImporterInfo,
            ManufacturerInfo = domain.ManufacturerInfo,
            ImageUrl = domain.ImageUrl,
            Notes = domain.Notes,
            Tags = domain.Tags,
            Code = domain.Code
        };
    }

    public DomainCategory ToDomainCategory(CoreCategory core)
    {
        ArgumentNullException.ThrowIfNull(core);

        return new DomainCategory
        {
            Name = core.Name ?? string.Empty,
            Description = core.Description,
            Code = core.Code ?? string.Empty,
            ImageUrl = core.ImageUrl,
            Color = core.Color,
            Icon = core.Icon,
            SortOrder = core.SortOrder,
            IsActive = core.IsActive,
            ShowInMenu = core.ShowInMenu
        };
    }

    public CoreCategory ToCoreCategory(DomainCategory domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CoreCategory
        {
            Name = domain.Name,
            Description = domain.Description ?? string.Empty,
            Code = domain.Code,
            ImageUrl = domain.ImageUrl,
            Color = domain.Color,
            Icon = domain.Icon,
            SortOrder = domain.SortOrder,
            IsActive = domain.IsActive,
            ShowInMenu = domain.ShowInMenu
        };
    }
}
#pragma warning restore CS0618
