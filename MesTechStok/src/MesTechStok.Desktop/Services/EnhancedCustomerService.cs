using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    public class EnhancedCustomerService
    {
        private readonly List<CustomerItem> _allCustomers;
        private readonly Random _random = new();

        public EnhancedCustomerService()
        {
            _allCustomers = GenerateCustomerData();
        }

        #region Public Methods

        public async Task<PagedResult<CustomerItem>> GetCustomersPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? searchTerm = null,
            CustomerTypeFilter typeFilter = CustomerTypeFilter.All,
            CustomerSortOrder sortOrder = CustomerSortOrder.FullName)
        {
            await Task.Delay(35); // Simulate network delay

            var filteredCustomers = FilterCustomers(searchTerm, typeFilter);
            var sortedCustomers = SortCustomers(filteredCustomers, sortOrder);

            var totalItems = sortedCustomers.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = sortedCustomers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<CustomerItem>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<CustomerStatistics> GetCustomerStatisticsAsync()
        {
            await Task.Delay(60);

            var totalCustomers = _allCustomers.Count;
            var activeCustomers = _allCustomers.Count(c => c.IsActive);
            var vipCustomers = _allCustomers.Count(c => c.CustomerType == "VIP");
            var newCustomersThisMonth = _allCustomers.Count(c => c.RegistrationDate >= DateTime.Now.AddDays(-30));
            var totalSalesValue = _allCustomers.Sum(c => c.TotalPurchases);

            return new CustomerStatistics
            {
                TotalCustomers = totalCustomers,
                ActiveCustomers = activeCustomers,
                VipCustomers = vipCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,
                TotalSalesValue = totalSalesValue,
                AverageOrderValue = totalSalesValue / Math.Max(totalCustomers, 1)
            };
        }

        #endregion

        #region Private Methods

        private IEnumerable<CustomerItem> FilterCustomers(string? searchTerm, CustomerTypeFilter typeFilter)
        {
            var customers = _allCustomers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                customers = customers.Where(c =>
                    c.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Company.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            customers = typeFilter switch
            {
                CustomerTypeFilter.Individual => customers.Where(c => c.CustomerType == "Bireysel"),
                CustomerTypeFilter.Corporate => customers.Where(c => c.CustomerType == "Kurumsal"),
                CustomerTypeFilter.VIP => customers.Where(c => c.CustomerType == "VIP"),
                CustomerTypeFilter.Active => customers.Where(c => c.IsActive),
                CustomerTypeFilter.Inactive => customers.Where(c => !c.IsActive),
                _ => customers
            };

            return customers;
        }

        private IEnumerable<CustomerItem> SortCustomers(IEnumerable<CustomerItem> customers, CustomerSortOrder sortOrder)
        {
            return sortOrder switch
            {
                CustomerSortOrder.FullName => customers.OrderBy(c => c.FullName),
                CustomerSortOrder.FullNameDesc => customers.OrderByDescending(c => c.FullName),
                CustomerSortOrder.RegistrationDate => customers.OrderBy(c => c.RegistrationDate),
                CustomerSortOrder.RegistrationDateDesc => customers.OrderByDescending(c => c.RegistrationDate),
                CustomerSortOrder.TotalPurchases => customers.OrderBy(c => c.TotalPurchases),
                CustomerSortOrder.TotalPurchasesDesc => customers.OrderByDescending(c => c.TotalPurchases),
                CustomerSortOrder.CustomerType => customers.OrderBy(c => c.CustomerType).ThenBy(c => c.FullName),
                _ => customers.OrderBy(c => c.FullName)
            };
        }

        private List<CustomerItem> GenerateCustomerData()
        {
            var customers = new List<CustomerItem>();
            var firstNames = new[] { "Ahmet", "Mehmet", "Mustafa", "Ali", "Hüseyin", "İbrahim", "İsmail", "Ömer", "Osman", "Murat", "Ayşe", "Fatma", "Emine", "Hatice", "Zeynep", "Elif", "Meryem", "Özlem", "Sevgi", "Gül" };
            var lastNames = new[] { "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Arslan", "Koç", "Doğan", "Türk", "Avcı", "Güler", "Polat", "Kara", "Özkan", "Aksoy", "Yalçın", "Kılıç", "Bayram", "Özdemir", "Erdoğan" };
            var companies = new[] { "ABC Ltd. Şti.", "XYZ A.Ş.", "Global Ticaret", "Teknoloji Plus", "İnşaat Grup", "Medya Ajans", "Pazarlama Uzmanı", "Lojistik Hizmet", "Danışmanlık Merkezi", "E-Ticaret Pro" };
            var customerTypes = new[] { "Bireysel", "Kurumsal", "VIP" };

            for (int i = 1; i <= 200; i++)
            {
                var firstName = firstNames[_random.Next(firstNames.Length)];
                var lastName = lastNames[_random.Next(lastNames.Length)];
                var customerType = customerTypes[_random.Next(customerTypes.Length)];
                var registrationDate = DateTime.Now.AddDays(-_random.Next(0, 1095)); // Son 3 yıl

                var customer = new CustomerItem
                {
                    Id = i,
                    FullName = $"{firstName} {lastName}",
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}@email.com",
                    PhoneNumber = $"0{_random.Next(500, 600)} {_random.Next(100, 999)} {_random.Next(10, 99)} {_random.Next(10, 99)}",
                    Company = customerType == "Kurumsal" ? companies[_random.Next(companies.Length)] : "",
                    CustomerType = customerType,
                    RegistrationDate = registrationDate,
                    LastOrderDate = registrationDate.AddDays(_random.Next(0, (DateTime.Now - registrationDate).Days)),
                    TotalPurchases = (decimal)(_random.Next(100, 50000) + _random.NextDouble()),
                    IsActive = _random.Next(0, 100) < 85 // %85 aktif müşteri
                };

                customers.Add(customer);
            }

            return customers;
        }

        #endregion
    }

    #region Supporting Classes

    public class CustomerItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Company { get; set; } = "";
        public string CustomerType { get; set; } = "";
        public DateTime RegistrationDate { get; set; }
        public DateTime LastOrderDate { get; set; }
        public decimal TotalPurchases { get; set; }
        public bool IsActive { get; set; }

        public string StatusIcon => IsActive ? "✅" : "⭕";
        public string FormattedTotalPurchases => $"₺{TotalPurchases:N2}";
        public string FormattedRegistrationDate => RegistrationDate.ToString("dd.MM.yyyy");
        public string FormattedLastOrderDate => LastOrderDate.ToString("dd.MM.yyyy");
        public int DaysSinceRegistration => (DateTime.Now - RegistrationDate).Days;
        public int DaysSinceLastOrder => (DateTime.Now - LastOrderDate).Days;
    }

    public class CustomerStatistics
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int VipCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public decimal TotalSalesValue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public enum CustomerTypeFilter
    {
        All,
        Individual,
        Corporate,
        VIP,
        Active,
        Inactive
    }

    public enum CustomerSortOrder
    {
        FullName,
        FullNameDesc,
        RegistrationDate,
        RegistrationDateDesc,
        TotalPurchases,
        TotalPurchasesDesc,
        CustomerType
    }

    #endregion
}