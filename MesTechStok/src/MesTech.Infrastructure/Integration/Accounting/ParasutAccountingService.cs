using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Accounting;

/// <summary>
/// Paraşüt muhasebe entegrasyonu — OAuth2 + JSON:API format.
/// Base URL: https://api.parasut.com/v4/{company_id}/
/// Gelir/gider kayıtlarını Paraşüt'e iletir; bakiye ve hareketleri çeker.
/// </summary>
public class ParasutAccountingService : IParasutAccountingService
{
    private readonly HttpClient _httpClient;
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly ILogger<ParasutAccountingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ParasutAccountingService(
        HttpClient httpClient,
        IIncomeRepository incomeRepository,
        IExpenseRepository expenseRepository,
        ILogger<ParasutAccountingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
        _expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ParasutSyncResult> PushIncomeAsync(Guid incomeId, CancellationToken ct = default)
    {
        _logger.LogInformation("Parasut PushIncome for income {IncomeId}", incomeId);

        var income = await _incomeRepository.GetByIdAsync(incomeId);
        if (income is null)
        {
            _logger.LogWarning("Parasut PushIncome: income {IncomeId} not found", incomeId);
            return new ParasutSyncResult { Success = false, ErrorMessage = $"Income {incomeId} not found." };
        }

        var payload = new
        {
            data = new
            {
                type = "sales_invoices",
                attributes = new
                {
                    description = income.Description,
                    issue_date = income.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    due_date = income.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    currency = "TRL",
                    net_total = income.Amount.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        };

        return await PostJsonApiAsync("sales_invoices", payload, ct);
    }

    /// <inheritdoc/>
    public async Task<ParasutSyncResult> PushExpenseAsync(Guid expenseId, CancellationToken ct = default)
    {
        _logger.LogInformation("Parasut PushExpense for expense {ExpenseId}", expenseId);

        var expense = await _expenseRepository.GetByIdAsync(expenseId);
        if (expense is null)
        {
            _logger.LogWarning("Parasut PushExpense: expense {ExpenseId} not found", expenseId);
            return new ParasutSyncResult { Success = false, ErrorMessage = $"Expense {expenseId} not found." };
        }

        var payload = new
        {
            data = new
            {
                type = "purchase_invoices",
                attributes = new
                {
                    description = expense.Description,
                    issue_date = expense.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    due_date = expense.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    currency = "TRL",
                    net_total = expense.Amount.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        };

        return await PostJsonApiAsync("purchase_invoices", payload, ct);
    }

    /// <inheritdoc/>
    public async Task<ParasutBalanceDto> GetBalanceAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Parasut GetBalance");

        try
        {
            var response = await _httpClient.GetAsync("accounts", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Parasut GetBalance failed: {Status} — {Error}",
                    response.StatusCode, errorBody);

                // Return zero balance on failure — caller can check logs
                return new ParasutBalanceDto { AsOf = DateTime.UtcNow };
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            // JSON:API: { data: [ { attributes: { balance, ... } } ] }
            decimal totalReceivable = 0m;
            decimal totalPayable = 0m;

            if (doc.RootElement.TryGetProperty("data", out var dataArray)
                && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var account in dataArray.EnumerateArray())
                {
                    if (!account.TryGetProperty("attributes", out var attrs))
                        continue;

                    if (attrs.TryGetProperty("balance", out var balProp)
                        && balProp.ValueKind != JsonValueKind.Null)
                    {
                        var balStr = balProp.GetString() ?? balProp.ToString();
                        if (decimal.TryParse(balStr, NumberStyles.Any,
                                CultureInfo.InvariantCulture, out var bal))
                        {
                            if (bal >= 0) totalReceivable += bal;
                            else totalPayable += Math.Abs(bal);
                        }
                    }
                }
            }

            return new ParasutBalanceDto
            {
                TotalReceivable = totalReceivable,
                TotalPayable = totalPayable,
                NetBalance = totalReceivable - totalPayable,
                AsOf = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Parasut GetBalance HTTP exception");
            return new ParasutBalanceDto { AsOf = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut GetBalance unexpected exception");
            return new ParasutBalanceDto { AsOf = DateTime.UtcNow };
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ParasutTransactionDto>> GetRecentTransactionsAsync(
        int days = 30, CancellationToken ct = default)
    {
        _logger.LogInformation("Parasut GetRecentTransactions for last {Days} days", days);

        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var url = $"transaction_documents?filter[issue_date_gte]={fromDate}&filter[issue_date_lte]={toDate}";
            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Parasut GetRecentTransactions failed: {Status} — {Error}",
                    response.StatusCode, errorBody);
                return Array.Empty<ParasutTransactionDto>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var transactions = new List<ParasutTransactionDto>();

            if (!doc.RootElement.TryGetProperty("data", out var dataArray)
                || dataArray.ValueKind != JsonValueKind.Array)
            {
                return transactions;
            }

            foreach (var item in dataArray.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;

                if (!item.TryGetProperty("attributes", out var attrs))
                    continue;

                var type = attrs.TryGetProperty("item_type", out var typeProp)
                    ? typeProp.GetString() ?? string.Empty : string.Empty;

                decimal amount = 0m;
                if (attrs.TryGetProperty("net_total", out var amtProp)
                    && amtProp.ValueKind != JsonValueKind.Null)
                {
                    var amtStr = amtProp.GetString() ?? amtProp.ToString();
                    decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
                }

                var description = attrs.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? string.Empty : string.Empty;

                DateTime date = DateTime.UtcNow;
                if (attrs.TryGetProperty("issue_date", out var dateProp)
                    && dateProp.ValueKind != JsonValueKind.Null)
                {
                    var dateStr = dateProp.GetString();
                    if (!string.IsNullOrEmpty(dateStr))
                        DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal, out date);
                }

                transactions.Add(new ParasutTransactionDto
                {
                    Id = id,
                    Type = type,
                    Amount = amount,
                    Description = description,
                    Date = date
                });
            }

            return transactions;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Parasut GetRecentTransactions HTTP exception");
            return Array.Empty<ParasutTransactionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut GetRecentTransactions unexpected exception");
            return Array.Empty<ParasutTransactionDto>();
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task<ParasutSyncResult> PostJsonApiAsync(string endpoint, object payload, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.api+json");

            var response = await _httpClient.PostAsync(endpoint, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Parasut POST {Endpoint} failed: {Status} — {Error}",
                    endpoint, response.StatusCode, errorBody);
                return new ParasutSyncResult { Success = false, ErrorMessage = errorBody };
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            // JSON:API: { data: { id, ... } }
            var externalId = doc.RootElement.TryGetProperty("data", out var dataProp)
                && dataProp.TryGetProperty("id", out var idProp)
                ? idProp.GetString()
                : null;

            _logger.LogInformation("Parasut POST {Endpoint} succeeded, external ID: {ExternalId}",
                endpoint, externalId);

            return new ParasutSyncResult { Success = true, ExternalId = externalId };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Parasut POST {Endpoint} HTTP exception", endpoint);
            return new ParasutSyncResult { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parasut POST {Endpoint} unexpected exception", endpoint);
            return new ParasutSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
