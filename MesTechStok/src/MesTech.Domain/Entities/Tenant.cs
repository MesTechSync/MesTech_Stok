using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Multi-tenant kiracı entity'si.
/// Her müşteri firma bir Tenant'tır.
/// </summary>
public sealed class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
