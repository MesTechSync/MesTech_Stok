using System.Net.Http.Json;

namespace MesTech.Blazor.Services;

/// <summary>
/// HTTP client for MesTech WebAPI.
/// Every method falls back gracefully — callers wrap calls in try/catch
/// and use demo data on failure.
/// </summary>
public sealed class MesTechApiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public MesTechApiClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _baseUrl = config["WebApi:BaseUrl"] ?? "http://localhost:3100";
    }

    // ── Public generic methods returning ApiResult<T> ──

    /// <summary>GET /api/v1/{endpoint} with graceful fallback.</summary>
    public async Task<ApiResult<T>> SafeGetAsync<T>(string endpoint) where T : class
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/api/v1/{endpoint}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                if (data is not null)
                    return ApiResult<T>.Success(data);
                return ApiResult<T>.Fallback("Sunucudan bos yanit alindi");
            }
            return ApiResult<T>.Fallback($"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fallback("Sunucu baglantisi kurulamadi");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Fallback("Istek zaman asimina ugradi");
        }
    }

    /// <summary>POST /api/v1/{endpoint} with graceful fallback.</summary>
    public async Task<ApiResult<T>> SafePostAsync<T>(string endpoint, object payload) where T : class
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/{endpoint}", payload);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                if (data is not null)
                    return ApiResult<T>.Success(data);
                return ApiResult<T>.Fallback("Sunucudan bos yanit alindi");
            }
            return ApiResult<T>.Fallback($"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fallback("Sunucu baglantisi kurulamadi");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Fallback("Istek zaman asimina ugradi");
        }
    }

    /// <summary>PUT /api/v1/{endpoint} with graceful fallback.</summary>
    public async Task<ApiResult<T>> SafePutAsync<T>(string endpoint, object payload) where T : class
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/api/v1/{endpoint}", payload);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                if (data is not null)
                    return ApiResult<T>.Success(data);
                return ApiResult<T>.Fallback("Sunucudan bos yanit alindi");
            }
            return ApiResult<T>.Fallback($"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fallback("Sunucu baglantisi kurulamadi");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Fallback("Istek zaman asimina ugradi");
        }
    }

    /// <summary>DELETE /api/v1/{endpoint} with graceful fallback.</summary>
    public async Task<ApiResult<DeleteResult>> SafeDeleteAsync(string endpoint)
    {
        try
        {
            var response = await _http.DeleteAsync($"{_baseUrl}/api/v1/{endpoint}");
            if (response.IsSuccessStatusCode)
                return ApiResult<DeleteResult>.Success(new DeleteResult(true));
            return ApiResult<DeleteResult>.Fallback($"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return ApiResult<DeleteResult>.Fallback("Sunucu baglantisi kurulamadi");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<DeleteResult>.Fallback("Istek zaman asimina ugradi");
        }
    }

    /// <summary>Simple wrapper for delete operation results (satisfies <c>where T : class</c> constraint).</summary>
    public sealed record DeleteResult(bool IsDeleted);

    // ── Legacy generic helpers (kept for backward compatibility) ──

    private async Task<T> GetAsync<T>(string path) where T : new()
    {
        var response = await _http.GetAsync($"{_baseUrl}{path}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>() ?? new T();
    }

    private async Task<T?> GetNullableAsync<T>(string path) where T : class
    {
        var response = await _http.GetAsync($"{_baseUrl}{path}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task PostAsync<T>(string path, T body)
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}{path}", body);
        response.EnsureSuccessStatusCode();
    }

    private async Task<TResult> PostAsync<T, TResult>(string path, T body) where TResult : new()
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}{path}", body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResult>() ?? new TResult();
    }

    // ── E-Invoice ──

    public Task<List<object>> GetEInvoicesAsync(int page = 1)
        => GetAsync<List<object>>($"/api/v1/invoices?page={page}");

    public Task<object?> GetEInvoiceByIdAsync(Guid id)
        => GetNullableAsync<object>($"/api/v1/invoices/{id}");

    public Task CreateEInvoiceAsync(object invoice)
        => PostAsync("/api/v1/invoices", invoice);

    public Task SendEInvoiceAsync(Guid id)
        => PostAsync($"/api/v1/invoices/{id}/send", new { });

    public Task CancelEInvoiceAsync(Guid id)
        => PostAsync($"/api/v1/invoices/{id}/cancel", new { });

    public Task<object?> CheckVknAsync(string vkn)
        => GetNullableAsync<object>($"/api/v1/invoices/check-vkn/{vkn}");

    // ── Categories ──

    public Task<List<object>> GetCategoriesAsync()
        => GetAsync<List<object>>("/api/v1/categories");

    // ── Platform ──

    public Task<List<object>> GetPlatformSyncStatusAsync()
        => GetAsync<List<object>>("/api/v1/sync-status");

    public Task<List<object>> GetPlatformOverviewAsync()
        => GetAsync<List<object>>("/api/v1/platforms");

    public Task TriggerPlatformSyncAsync(string platform)
        => PostAsync($"/api/v1/sync/{platform}", new { });

    // ── Shipping ──

    public Task<List<object>> GetShipmentQueueAsync()
        => GetAsync<List<object>>("/api/v1/shipping/queue");

    public Task<object?> GetShipmentTrackingAsync(string trackingNumber)
        => GetNullableAsync<object>($"/api/v1/shipping/track/{trackingNumber}");

    public Task<List<object>> GetCargoComparisonAsync()
        => GetAsync<List<object>>("/api/v1/shipping/comparison");

    public Task SendShipmentAsync(Guid id, string cargoFirm)
        => PostAsync("/api/v1/shipping/send", new { Id = id, CargoFirm = cargoFirm });

    // ── Dropshipping ──

    public Task<object?> GetDropshippingDashboardAsync()
        => GetNullableAsync<object>("/api/v1/dropshipping/dashboard");

    public Task<List<object>> GetDropshippingPoolAsync()
        => GetAsync<List<object>>("/api/v1/dropshipping/pool");

    public Task<List<object>> GetDropshippingSuppliersAsync()
        => GetAsync<List<object>>("/api/v1/dropshipping/suppliers");

    // ── Quotations ──

    public Task<List<object>> GetQuotationsAsync()
        => GetAsync<List<object>>("/api/v1/quotations");

    // ── Cari Hesaplar ──

    public Task<List<object>> GetCariHesaplarAsync()
        => GetAsync<List<object>>("/api/v1/cari");

    public Task<List<object>> GetCariHareketlerAsync(int cariId)
        => GetAsync<List<object>>($"/api/v1/cari/{cariId}/hareketler");

    // ── Reconciliation ──

    public Task<List<object>> GetReconciliationMatchesAsync()
        => GetAsync<List<object>>("/api/v1/reconciliation/matches");

    public Task RunReconciliationAsync()
        => PostAsync("/api/v1/reconciliation/run", new { });

    // ── Admin — Tenants ──

    public Task<List<object>> GetTenantsAsync()
        => GetAsync<List<object>>("/api/v1/admin/tenants");

    public Task<object?> GetTenantByIdAsync(Guid id)
        => GetNullableAsync<object>($"/api/v1/admin/tenants/{id}");

    // ── Admin — Stores ──

    public Task<List<object>> GetStoresAsync()
        => GetAsync<List<object>>("/api/v1/admin/stores");

    // ── Warehouses ──

    public Task<List<object>> GetWarehousesAsync()
        => GetAsync<List<object>>("/api/v1/warehouses");

    public Task<object?> GetWarehouseByIdAsync(Guid id)
        => GetNullableAsync<object>($"/api/v1/warehouses/{id}");

    // ── System Health ──

    public Task<object?> GetSystemHealthAsync()
        => GetNullableAsync<object>("/api/v1/admin/health");

    // ── Calendar ──

    public Task<List<object>> GetCalendarEventsAsync(int year, int month)
        => GetAsync<List<object>>($"/api/v1/calendar/events?year={year}&month={month}");

    // ── Projects ──

    public Task<List<object>> GetProjectsAsync()
        => GetAsync<List<object>>("/api/v1/projects");

    // ── Notifications ──

    public Task<List<object>> GetNotificationsAsync()
        => GetAsync<List<object>>("/api/v1/notifications");

    // ── Barcodes ──

    public Task<object?> GetProductByBarcodeAsync(string barcode)
        => GetNullableAsync<object>($"/api/v1/barcodes/lookup/{barcode}");

    public Task<List<object>> GetBarcodeScanLogsAsync()
        => GetAsync<List<object>>("/api/v1/barcodes/scan-logs");

    // ── Shipments (Group E) ──

    public Task<List<object>> GetShipmentsAsync()
        => GetAsync<List<object>>("/api/v1/shipments");

    public Task CreateShipmentAsync(object shipment)
        => PostAsync("/api/v1/shipments", shipment);

    public Task<object?> GetShipmentTrackingByIdAsync(Guid id)
        => GetNullableAsync<object>($"/api/v1/shipments/{id}/tracking");

    public Task<List<object>> GetCargoProvidersAsync()
        => GetAsync<List<object>>("/api/v1/cargo/providers");

    public Task<object?> GetShipmentLabelAsync(Guid id)
        => GetNullableAsync<object>($"/api/v1/shipments/{id}/label");

    // ── Invoices & Finance (Group F) ──

    public Task<List<object>> GetInvoicesAsync()
        => GetAsync<List<object>>("/api/v1/invoices");

    public Task CreateInvoiceAsync(object invoice)
        => PostAsync("/api/v1/invoices", invoice);

    public Task<List<object>> GetInvoiceProvidersAsync()
        => GetAsync<List<object>>("/api/v1/invoice-providers");

    public Task<List<object>> GetSettlementsAsync()
        => GetAsync<List<object>>("/api/v1/settlements");

    public Task<List<object>> GetPaymentsAsync()
        => GetAsync<List<object>>("/api/v1/payments");

    public Task<List<object>> GetCommissionsAsync()
        => GetAsync<List<object>>("/api/v1/commissions");

    public Task<object?> GetFinancialReportsAsync()
        => GetNullableAsync<object>("/api/v1/reports/financial");

    // ── Accounting (Group G) ──

    public Task<List<object>> GetTrialBalanceAsync()
        => GetAsync<List<object>>("/api/v1/accounting/trial-balance");

    public Task<List<object>> GetBalanceSheetAsync()
        => GetAsync<List<object>>("/api/v1/accounting/balance-sheet");

    public Task<object?> GetProfitReportAsync()
        => GetNullableAsync<object>("/api/v1/accounting/profit-report");

    public Task<List<object>> GetChartOfAccountsAsync()
        => GetAsync<List<object>>("/api/v1/accounting/chart-of-accounts");

    public Task<List<object>> GetJournalEntriesAsync()
        => GetAsync<List<object>>("/api/v1/accounting/journal-entries");

    public Task<List<object>> GetCommissionRatesAsync()
        => GetAsync<List<object>>("/api/v1/accounting/commission-rates");

    public Task<object?> GetErpSyncStatusAsync()
        => GetNullableAsync<object>("/api/v1/erp/sync-status");

    // ── Dashboard & Settings (Group H) ──

    public Task<object?> GetDashboardSummaryAsync()
        => GetNullableAsync<object>("/api/v1/dashboard/summary");

    public Task<object?> GetDashboardKpiAsync()
        => GetNullableAsync<object>("/api/v1/dashboard/kpi");

    public Task<object?> GetUserSettingsAsync()
        => GetNullableAsync<object>("/api/v1/users/me/settings");

    public Task UpdateUserSettingsAsync(object settings)
        => PostAsync("/api/v1/users/me/settings", settings);

    public Task<object?> GetTenantSettingsAsync()
        => GetNullableAsync<object>("/api/v1/tenants/current");

    public Task UpdateTenantSettingsAsync(object settings)
        => PostAsync("/api/v1/tenants/current", settings);

    public Task<object?> GetMesaStatusAsync()
        => GetNullableAsync<object>("/api/v1/mesa/status");

    public Task<object?> GetHealthAsync()
        => GetNullableAsync<object>("/api/v1/health");

    // ── Settings (EMR-15-P) ──

    public Task<object?> GetSettingsProfileAsync()
        => GetNullableAsync<object>("/api/v1/settings/profile");

    public Task<List<object>> GetSettingsCredentialsAsync()
        => GetAsync<List<object>>("/api/v1/settings/credentials");

    public Task UpdateSettingsProfileAsync(object profile)
        => PostAsync("/api/v1/settings/profile", profile);

    private async Task<T> PutAsync<T>(string path, T body) where T : new()
    {
        var response = await _http.PutAsJsonAsync($"{_baseUrl}{path}", body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>() ?? new T();
    }
}
