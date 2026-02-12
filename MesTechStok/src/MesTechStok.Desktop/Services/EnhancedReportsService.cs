using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    public class EnhancedReportsService
    {
        private readonly List<ReportItem> _allReports;
        private readonly Random _random = new();

        public EnhancedReportsService()
        {
            _allReports = GenerateReportData();
        }

        #region Public Methods

        public async Task<PagedResult<ReportItem>> GetReportsPagedAsync(
            int page = 1,
            int pageSize = 25,
            string? searchTerm = null,
            ReportTypeFilter typeFilter = ReportTypeFilter.All,
            ReportSortOrder sortOrder = ReportSortOrder.CreatedDateDesc)
        {
            await Task.Delay(60); // Simulate network delay for report generation

            var filteredReports = FilterReports(searchTerm, typeFilter);
            var sortedReports = SortReports(filteredReports, sortOrder);

            var totalItems = sortedReports.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = sortedReports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<ReportItem>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<ReportStatistics> GetReportStatisticsAsync()
        {
            await Task.Delay(100);

            var totalReports = _allReports.Count;
            var dailyReports = _allReports.Count(r => r.CreatedDate.Date == DateTime.Today);
            var weeklyReports = _allReports.Count(r => r.CreatedDate >= DateTime.Now.AddDays(-7));
            var monthlyReports = _allReports.Count(r => r.CreatedDate >= DateTime.Now.AddDays(-30));
            var automatedReports = _allReports.Count(r => r.IsAutomated);

            return new ReportStatistics
            {
                TotalReports = totalReports,
                DailyReports = dailyReports,
                WeeklyReports = weeklyReports,
                MonthlyReports = monthlyReports,
                AutomatedReports = automatedReports
            };
        }

        public async Task<bool> GenerateReportAsync(string reportType, string reportName, Dictionary<string, object> parameters)
        {
            await Task.Delay(2000); // Simulate report generation time

            var newReport = new ReportItem
            {
                Id = _allReports.Max(r => r.Id) + 1,
                ReportName = reportName,
                ReportType = reportType,
                CreatedDate = DateTime.Now,
                Status = "Tamamlandı",
                FileSize = $"{_random.Next(50, 500)} KB",
                IsAutomated = false,
                CreatedBy = "Manuel Kullanıcı"
            };

            _allReports.Insert(0, newReport);
            return true;
        }

        #endregion

        #region Private Methods

        private IEnumerable<ReportItem> FilterReports(string? searchTerm, ReportTypeFilter typeFilter)
        {
            var reports = _allReports.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                reports = reports.Where(r =>
                    r.ReportName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.ReportType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.CreatedBy.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            reports = typeFilter switch
            {
                ReportTypeFilter.Sales => reports.Where(r => r.ReportType.Contains("Satış")),
                ReportTypeFilter.Inventory => reports.Where(r => r.ReportType.Contains("Stok")),
                ReportTypeFilter.Financial => reports.Where(r => r.ReportType.Contains("Mali")),
                ReportTypeFilter.Customer => reports.Where(r => r.ReportType.Contains("Müşteri")),
                ReportTypeFilter.Automated => reports.Where(r => r.IsAutomated),
                ReportTypeFilter.Manual => reports.Where(r => !r.IsAutomated),
                _ => reports
            };

            return reports;
        }

        private IEnumerable<ReportItem> SortReports(IEnumerable<ReportItem> reports, ReportSortOrder sortOrder)
        {
            return sortOrder switch
            {
                ReportSortOrder.ReportName => reports.OrderBy(r => r.ReportName),
                ReportSortOrder.ReportNameDesc => reports.OrderByDescending(r => r.ReportName),
                ReportSortOrder.CreatedDate => reports.OrderBy(r => r.CreatedDate),
                ReportSortOrder.CreatedDateDesc => reports.OrderByDescending(r => r.CreatedDate),
                ReportSortOrder.ReportType => reports.OrderBy(r => r.ReportType).ThenBy(r => r.ReportName),
                ReportSortOrder.CreatedBy => reports.OrderBy(r => r.CreatedBy).ThenByDescending(r => r.CreatedDate),
                _ => reports.OrderByDescending(r => r.CreatedDate)
            };
        }

        private List<ReportItem> GenerateReportData()
        {
            var reports = new List<ReportItem>();
            var reportTypes = new[] { "Satış Raporu", "Stok Raporu", "Mali Rapor", "Müşteri Raporu", "Ürün Analizi", "Performans Raporu" };
            var reportNames = new[]
            {
                "Günlük Satış Özeti", "Haftalık Stok Durumu", "Aylık Gelir Tablosu", "Müşteri Memnuniyet Anketi",
                "En Çok Satan Ürünler", "Kritik Stok Uyarıları", "Kar-Zarar Analizi", "Yeni Müşteri Kayıtları",
                "Kategori Bazlı Satışlar", "Tedarikçi Performansı", "Sezonsal Trend Analizi", "Fiyat Karşılaştırması"
            };
            var creators = new[] { "System Auto", "Admin User", "Manager", "Analyst", "Reporter" };

            for (int i = 1; i <= 75; i++)
            {
                var createdDate = DateTime.Now.AddDays(-_random.Next(0, 90));
                var reportType = reportTypes[_random.Next(reportTypes.Length)];
                var reportName = reportNames[_random.Next(reportNames.Length)];
                var creator = creators[_random.Next(creators.Length)];
                var isAutomated = creator == "System Auto";

                var report = new ReportItem
                {
                    Id = i,
                    ReportName = $"{reportName} #{i:D3}",
                    ReportType = reportType,
                    CreatedDate = createdDate,
                    Status = _random.Next(0, 100) < 95 ? "Tamamlandı" : "İşleniyor",
                    FileSize = $"{_random.Next(25, 1500)} KB",
                    IsAutomated = isAutomated,
                    CreatedBy = creator
                };

                reports.Add(report);
            }

            return reports.OrderByDescending(r => r.CreatedDate).ToList();
        }

        #endregion
    }

    #region Supporting Classes

    public class ReportItem
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = "";
        public string ReportType { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "";
        public string FileSize { get; set; } = "";
        public bool IsAutomated { get; set; }
        public string CreatedBy { get; set; } = "";

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    "Tamamlandı" => "✅",
                    "İşleniyor" => "🔄",
                    "Beklemede" => "⏳",
                    "Hata" => "❌",
                    _ => "📄"
                };
            }
        }

        public string TypeIcon
        {
            get
            {
                return ReportType switch
                {
                    var x when x.Contains("Satış") => "💰",
                    var x when x.Contains("Stok") => "📦",
                    var x when x.Contains("Mali") => "💹",
                    var x when x.Contains("Müşteri") => "👥",
                    var x when x.Contains("Ürün") => "🏷️",
                    var x when x.Contains("Performans") => "📊",
                    _ => "📄"
                };
            }
        }

        public string AutomationIcon => IsAutomated ? "🤖" : "👤";
        public string FormattedCreatedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");
        public string FormattedCreatedDateShort => CreatedDate.ToString("dd.MM.yyyy");
        public int DaysAgo => (DateTime.Now - CreatedDate).Days;
    }

    public class ReportStatistics
    {
        public int TotalReports { get; set; }
        public int DailyReports { get; set; }
        public int WeeklyReports { get; set; }
        public int MonthlyReports { get; set; }
        public int AutomatedReports { get; set; }
    }

    public enum ReportTypeFilter
    {
        All,
        Sales,
        Inventory,
        Financial,
        Customer,
        Automated,
        Manual
    }

    public enum ReportSortOrder
    {
        ReportName,
        ReportNameDesc,
        CreatedDate,
        CreatedDateDesc,
        ReportType,
        CreatedBy
    }

    #endregion
}