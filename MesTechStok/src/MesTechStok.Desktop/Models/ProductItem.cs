using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Media.Imaging;

namespace MesTechStok.Desktop.Models
{
    public class ProductItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name = "";
        private string _barcode = "";
        private string _category = "";
        private string _sku = "";
        private decimal _salePrice;
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
        private string? _additionalImageUrls;
        private string? _documentUrls;
        private string? _tags;
        private string? _origin;
        private string? _material;
        private string? _volumeText;
        private decimal? _desi;
        private int? _leadTimeDays;
        private string? _shipAddress;
        private string? _returnAddress;
        private string? _color;
        private string? _sizes; // comma-separated sizes
        private decimal? _lengthCm;
        private decimal? _widthCm;
        private decimal? _heightCm;
        private string? _usageInstructions;
        private string? _importerInfo;
        private string? _manufacturerInfo;
        private bool _linkOnlyCover;

        #region Properties

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Barcode
        {
            get => _barcode;
            set => SetField(ref _barcode, value);
        }

        public string Category
        {
            get => _category;
            set => SetField(ref _category, value);
        }

        public string Sku
        {
            get => _sku;
            set => SetField(ref _sku, value);
        }

        public decimal SalePrice
        {
            get => _salePrice;
            set { SetField(ref _salePrice, value); OnPropertyChanged(nameof(FormattedSalePrice)); OnPropertyChanged(nameof(FinalPrice)); OnPropertyChanged(nameof(FormattedFinalPrice)); }
        }

        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set { SetField(ref _purchasePrice, value); OnPropertyChanged(nameof(FormattedPurchasePrice)); }
        }

        // Geriye dönük uyumluluk
        public decimal Price
        {
            get => SalePrice;
            set => SalePrice = value;
        }

        public decimal Cost
        {
            get => PurchasePrice;
            set => PurchasePrice = value;
        }

        public decimal DiscountRate
        {
            get => _discountRate;
            set { SetField(ref _discountRate, Math.Max(0, Math.Min(100, value))); OnPropertyChanged(nameof(FinalPrice)); OnPropertyChanged(nameof(FormattedFinalPrice)); OnPropertyChanged(nameof(DiscountActive)); }
        }

        public int Stock
        {
            get => _stock;
            set
            {
                SetField(ref _stock, value);
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StockStatus));
                OnPropertyChanged(nameof(IsLowStock));
                OnPropertyChanged(nameof(IsCriticalStock));
                OnPropertyChanged(nameof(IsOutOfStock));
            }
        }

        public int MinimumStock
        {
            get => _minimumStock;
            set
            {
                SetField(ref _minimumStock, value);
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StockStatus));
                OnPropertyChanged(nameof(IsLowStock));
                OnPropertyChanged(nameof(IsCriticalStock));
            }
        }

        public string? Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public string Supplier
        {
            get => _supplier;
            set => SetField(ref _supplier, value);
        }

        public string Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetField(ref _createdDate, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetField(ref _lastUpdated, value);
        }

        public string? ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (SetField(ref _imageUrl, value))
                {
                    OnPropertyChanged(nameof(ImageSource));
                    OnPropertyChanged(nameof(GalleryUrls));
                    OnPropertyChanged(nameof(HasImage));
                    OnPropertyChanged(nameof(MissingImage));
                }
            }
        }

        public string? AdditionalImageUrls
        {
            get => _additionalImageUrls;
            set
            {
                if (SetField(ref _additionalImageUrls, value))
                {
                    OnPropertyChanged(nameof(GalleryUrls));
                    OnPropertyChanged(nameof(HasImage));
                    OnPropertyChanged(nameof(MissingImage));
                }
            }
        }

        public string? DocumentUrls
        {
            get => _documentUrls;
            set => SetField(ref _documentUrls, value);
        }

        public string? Tags
        {
            get => _tags;
            set => SetField(ref _tags, value);
        }

        public string? Origin
        {
            get => _origin;
            set => SetField(ref _origin, value);
        }

        public string? Material
        {
            get => _material;
            set => SetField(ref _material, value);
        }

        public string? VolumeText
        {
            get => _volumeText;
            set => SetField(ref _volumeText, value);
        }

        public decimal? Desi
        {
            get => _desi;
            set => SetField(ref _desi, value);
        }

        public int? LeadTimeDays
        {
            get => _leadTimeDays;
            set => SetField(ref _leadTimeDays, value);
        }

        public string? ShipAddress
        {
            get => _shipAddress;
            set => SetField(ref _shipAddress, value);
        }

        public string? ReturnAddress
        {
            get => _returnAddress;
            set => SetField(ref _returnAddress, value);
        }

        public string? Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        public string? Sizes
        {
            get => _sizes;
            set => SetField(ref _sizes, value);
        }

        public decimal? LengthCm
        {
            get => _lengthCm;
            set => SetField(ref _lengthCm, value);
        }

        public decimal? WidthCm
        {
            get => _widthCm;
            set => SetField(ref _widthCm, value);
        }

        public decimal? HeightCm
        {
            get => _heightCm;
            set => SetField(ref _heightCm, value);
        }

        public string? UsageInstructions
        {
            get => _usageInstructions;
            set => SetField(ref _usageInstructions, value);
        }

        public string? ImporterInfo
        {
            get => _importerInfo;
            set => SetField(ref _importerInfo, value);
        }

        public string? ManufacturerInfo
        {
            get => _manufacturerInfo;
            set => SetField(ref _manufacturerInfo, value);
        }

        public bool LinkOnlyCover
        {
            get => _linkOnlyCover;
            set => SetField(ref _linkOnlyCover, value);
        }

        #endregion

        #region Computed Properties

        public string Status
        {
            get
            {
                if (Stock == 0) return "Tükendi";
                if (Stock <= 5) return "Kritik";
                if (Stock <= MinimumStock) return "Düşük";
                return "Normal";
            }
        }

        public string StatusIcon
        {
            get
            {
                if (Stock == 0) return "❌";
                if (Stock <= 5) return "🚨";
                if (Stock <= MinimumStock) return "⚠️";
                return "✅";
            }
        }

        public StockStatus StockStatus
        {
            get
            {
                if (Stock == 0) return Models.StockStatus.OutOfStock;
                if (Stock <= 5) return Models.StockStatus.Critical;
                if (Stock <= MinimumStock) return Models.StockStatus.Low;
                return Models.StockStatus.Normal;
            }
        }

        public bool IsLowStock => Stock <= MinimumStock && Stock > 0;

        public bool IsCriticalStock => Stock <= 5;

        public bool IsOutOfStock => Stock == 0;

        public decimal TotalValue => SalePrice * Stock;

        public string FormattedSalePrice => $"₺{SalePrice:N2}";
        public string FormattedPurchasePrice => $"₺{PurchasePrice:N2}";

        public decimal FinalPrice => Math.Round(SalePrice * (1 - (DiscountRate / 100m)), 2);
        public string FormattedFinalPrice => $"₺{FinalPrice:N2}";

        public decimal MarginAmount => Math.Max(0, FinalPrice - PurchasePrice);
        public string MarginAmountDisplay => $"₺{MarginAmount:N2}";

        public decimal MarginPercent => FinalPrice > 0 ? Math.Round(100m * (FinalPrice - PurchasePrice) / FinalPrice, 2) : 0m;
        public string MarginPercentDisplay => $"%{MarginPercent:0.##}";

        public string FormattedTotalValue => $"₺{TotalValue:N2}";

        public string StockDisplay => $"{Stock} adet";

        public string MinimumStockDisplay => $"Min: {MinimumStock}";

        public string CreatedDateDisplay => CreatedDate.ToString("dd.MM.yyyy");

        public string LastUpdatedDisplay => LastUpdated.ToString("dd.MM.yyyy HH:mm");

        public int DaysSinceCreated => (DateTime.Now - CreatedDate).Days;

        public int DaysSinceLastUpdate => (DateTime.Now - LastUpdated).Days;

        public string CategoryIcon
        {
            get
            {
                return Category.ToLower() switch
                {
                    "elektronik" => "⚡",
                    "içecek" => "🥤",
                    "atıştırmalık" => "🍿",
                    "kozmetik" => "💄",
                    "spor" => "⚽",
                    "gıda" => "🍎",
                    "oyuncak" => "🧸",
                    "ev gereçleri" => "🏠",
                    "kırtasiye" => "✏️",
                    "sağlık" => "💊",
                    _ => "📦"
                };
            }
        }

        public string FullDisplayName => $"{CategoryIcon} {Name}";

        public string ShortName => Name.Length > 30 ? Name[..27] + "..." : Name;

        public BitmapImage? ImageSource
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(ImageUrl)) return null;
                    var path = ImageUrl!;
                    if (!Uri.TryCreate(path, UriKind.Absolute, out var uri) || uri.IsFile)
                    {
                        if (!Path.IsPathRooted(path))
                        {
                            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                        }
                        if (!File.Exists(path)) return null;
                        uri = new Uri(path);
                    }
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = uri;
                    bmp.DecodePixelWidth = 120; // küçük hücre önizleme (fallback)
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch { return null; }
            }
        }

        public BitmapImage? ThumbnailSource
        {
            get
            {
                try
                {
                    var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var thumb = Path.Combine(local, "MesTechStok", "Images", "Products", Id.ToString(), "thumb_128.jpg");
                    if (File.Exists(thumb))
                    {
                        var b = new BitmapImage();
                        b.BeginInit();
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.UriSource = new Uri(thumb);
                        b.EndInit();
                        b.Freeze();
                        return b;
                    }
                    return ImageSource;
                }
                catch { return ImageSource; }
            }
        }

        public BitmapImage? PreviewSource
        {
            get
            {
                try
                {
                    var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var p768 = Path.Combine(local, "MesTechStok", "Images", "Products", Id.ToString(), "preview_768.jpg");
                    var p256 = Path.Combine(local, "MesTechStok", "Images", "Products", Id.ToString(), "thumb_256.jpg");
                    string? pick = File.Exists(p768) ? p768 : (File.Exists(p256) ? p256 : null);
                    if (pick != null)
                    {
                        var b = new BitmapImage();
                        b.BeginInit();
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.UriSource = new Uri(pick);
                        b.EndInit();
                        b.Freeze();
                        return b;
                    }
                    return ImageSource;
                }
                catch { return ImageSource; }
            }
        }

        public IReadOnlyList<string> GalleryUrls
        {
            get
            {
                var list = new List<string>();
                if (!string.IsNullOrWhiteSpace(ImageUrl)) list.Add(ImageUrl!);
                if (!string.IsNullOrWhiteSpace(AdditionalImageUrls))
                {
                    var parts = AdditionalImageUrls!.Split(new[] { ';', ',', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var trimmed = p.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed) && !list.Contains(trimmed)) list.Add(trimmed);
                    }
                }
                return list;
            }
        }

        public bool DiscountActive => DiscountRate > 0;

        public bool HasImage
        {
            get
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(ImageUrl)) return true;
                    if (!string.IsNullOrWhiteSpace(AdditionalImageUrls)) return true;
                    var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var thumb = Path.Combine(local, "MesTechStok", "Images", "Products", Id.ToString(), "thumb_128.jpg");
                    var p768 = Path.Combine(local, "MesTechStok", "Images", "Products", Id.ToString(), "preview_768.jpg");
                    var p256 = Path.Combine(local, "MesTechStok", "Images", "Products", Id.ToString(), "thumb_256.jpg");
                    return File.Exists(thumb) || File.Exists(p768) || File.Exists(p256);
                }
                catch { return false; }
            }
        }

        public bool MissingImage => !HasImage;

        // Trendyol benzeri gösterimler için basit türetilmiş alanlar
        public int FillRatePercent
        {
            get
            {
                var denom = Math.Max(1, MinimumStock * 2);
                var pct = (int)Math.Round(100m * Stock / denom);
                return Math.Max(0, Math.Min(100, pct));
            }
        }

        public string FillRateLabel
        {
            get
            {
                int p = FillRatePercent;
                if (p < 50) return "Zayıf";
                if (p < 80) return "Orta";
                return "İyi";
            }
        }

        public decimal CommissionRate { get; set; } = 21.5m;
        public string CommissionDisplay => $"%{CommissionRate:0.##}";

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Methods

        public void UpdateStock(int newStock)
        {
            Stock = newStock;
            LastUpdated = DateTime.Now;
        }

        public void AdjustStock(int adjustment)
        {
            Stock = Math.Max(0, Stock + adjustment);
            LastUpdated = DateTime.Now;
        }

        public void UpdatePrice(decimal newPrice)
        {
            Price = newPrice;
            LastUpdated = DateTime.Now;
        }

        public ProductItem Clone()
        {
            return new ProductItem
            {
                Id = Id,
                Name = Name,
                Barcode = Barcode,
                Category = Category,
                Price = Price,
                PurchasePrice = PurchasePrice,
                DiscountRate = DiscountRate,
                Stock = Stock,
                MinimumStock = MinimumStock,
                Description = Description,
                Supplier = Supplier,
                Location = Location,
                CreatedDate = CreatedDate,
                LastUpdated = LastUpdated,
                Sku = Sku,
                ImageUrl = ImageUrl,
                AdditionalImageUrls = AdditionalImageUrls
            };
        }

        public override string ToString()
        {
            return $"{Name} ({Barcode}) - {Stock} adet";
        }

        public override bool Equals(object? obj)
        {
            if (obj is ProductItem other)
            {
                return Id == other.Id && Barcode == other.Barcode;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Barcode);
        }

        #endregion
    }

    public enum StockStatus
    {
        Normal,
        Low,
        Critical,
        OutOfStock
    }
}