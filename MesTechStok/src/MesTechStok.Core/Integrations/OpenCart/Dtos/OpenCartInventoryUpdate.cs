namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartInventoryUpdate
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public int NewQuantity { get; set; }
        public string UpdateReason { get; set; } = string.Empty;
        public DateTime UpdateDate { get; set; } = DateTime.Now;
    }
}
