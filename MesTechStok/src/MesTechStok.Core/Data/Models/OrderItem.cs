using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string ProductSKU { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0.18m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Calculated Properties
        [NotMapped]
        public decimal SubTotal => Quantity * UnitPrice;

        public void CalculateAmounts()
        {
            TaxAmount = (UnitPrice * Quantity) * (TaxRate / 100);
            TotalPrice = (UnitPrice * Quantity) + TaxAmount;
        }

        public override string ToString() => $"{ProductName} x{Quantity} - ₺{TotalPrice:N2}";
    }
}
