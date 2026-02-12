// 🛠️ **PRODUCT MODEL - Neural Integration Ready**
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MesTechStok.Core.Services.Neural;

namespace MesTechStok.Core.Models
{
    public class Product : INotifyPropertyChanged
    {
        private int _id;
        private string _name = "";
        private string _barcode = "";
        private string _category = "";
        private string _sku = "";
        private decimal _price;
        private decimal _purchasePrice;
        private decimal _discountRate;
        private int _stock;
        private int _minimumStock = 10;
        private string? _description;
        private string _supplier = "";
        private string _location = "";
        private DateTime _createdDate = DateTime.Now;
        private DateTime _lastUpdated = DateTime.Now;
        private string? _imageUrl;
        private ProductAIInsights? _aiInsights;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? "");
        }

        public string Barcode
        {
            get => _barcode;
            set => SetProperty(ref _barcode, value ?? "");
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value ?? "");
        }

        public string SKU
        {
            get => _sku;
            set => SetProperty(ref _sku, value ?? "");
        }

        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set => SetProperty(ref _purchasePrice, value);
        }

        public decimal DiscountRate
        {
            get => _discountRate;
            set => SetProperty(ref _discountRate, value);
        }

        public int Stock
        {
            get => _stock;
            set => SetProperty(ref _stock, value);
        }

        public int MinimumStock
        {
            get => _minimumStock;
            set => SetProperty(ref _minimumStock, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Supplier
        {
            get => _supplier;
            set => SetProperty(ref _supplier, value ?? "");
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value ?? "");
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public string? ImageUrl
        {
            get => _imageUrl;
            set => SetProperty(ref _imageUrl, value);
        }

        public ProductAIInsights? AIInsights
        {
            get => _aiInsights;
            set => SetProperty(ref _aiInsights, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // Repository Interface
    public interface IProductRepository
    {
        Task<Product[]> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<Product[]> SearchAsync(string query);
    }
}
