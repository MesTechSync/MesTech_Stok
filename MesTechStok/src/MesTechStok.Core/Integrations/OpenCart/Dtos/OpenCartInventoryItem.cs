namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartInventoryItem
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public bool InStock { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
