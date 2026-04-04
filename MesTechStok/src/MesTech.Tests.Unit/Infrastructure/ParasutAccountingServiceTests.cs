using System.Net;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Accounting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// ParasutAccountingService unit testleri.
/// G10 A-08: Paraşüt muhasebe entegrasyonu — HTTP transport via mock handler.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ParasutAccountingService")]
[Trait("Phase", "Dalga6")]
public class ParasutAccountingServiceTests
{
    private readonly Mock<IIncomeRepository> _incomeRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
    private readonly NullLogger<ParasutAccountingService> _logger = NullLogger<ParasutAccountingService>.Instance;

    // ── Helpers ──────────────────────────────────────────────────────────

    private static HttpClient BuildHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.parasut.com/v4/12345/")
        };
    }

    private ParasutAccountingService BuildService(HttpClient httpClient)
        => new(httpClient, _incomeRepoMock.Object, _expenseRepoMock.Object, _logger);

    private static Income MakeIncome(Guid id)
    {
        var income = new Income
        {
            TenantId = Guid.NewGuid(),
            Description = "Test Income",
            IncomeType = IncomeType.Satis,
            Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        income.SetAmount(1500m);
        return income;
    }

    private static Expense MakeExpense(Guid id)
    {
        var expense = new Expense
        {
            TenantId = Guid.NewGuid(),
            Description = "Test Expense",
            ExpenseType = ExpenseType.Kargo,
            Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        expense.SetAmount(800m);
        return expense;
    }

    // ── Constructor Guards ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        var act = () => new ParasutAccountingService(
            null!, _incomeRepoMock.Object, _expenseRepoMock.Object, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_NullIncomeRepository_ThrowsArgumentNullException()
    {
        var act = () => new ParasutAccountingService(
            new HttpClient(), null!, _expenseRepoMock.Object, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("incomeRepository");
    }

    [Fact]
    public void Constructor_NullExpenseRepository_ThrowsArgumentNullException()
    {
        var act = () => new ParasutAccountingService(
            new HttpClient(), _incomeRepoMock.Object, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("expenseRepository");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ParasutAccountingService(
            new HttpClient(), _incomeRepoMock.Object, _expenseRepoMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ── PushIncomeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task PushIncomeAsync_IncomeNotFound_ReturnsFalseResult()
    {
        var incomeId = Guid.NewGuid();
        _incomeRepoMock.Setup(r => r.GetByIdAsync(incomeId, It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);

        var service = BuildService(new HttpClient());
        var result = await service.PushIncomeAsync(incomeId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain(incomeId.ToString());
    }

    [Fact]
    public async Task PushIncomeAsync_IncomeFound_HttpSuccess_ReturnsTrueWithExternalId()
    {
        var incomeId = Guid.NewGuid();
        var income = MakeIncome(incomeId);
        _incomeRepoMock.Setup(r => r.GetByIdAsync(incomeId, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var responseJson = """
            {
                "data": {
                    "id": "parasut-123",
                    "type": "sales_invoices",
                    "attributes": { "status": "draft" }
                }
            }
            """;

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseJson)
        });

        var service = BuildService(httpClient);
        var result = await service.PushIncomeAsync(incomeId);

        result.Success.Should().BeTrue();
        result.ExternalId.Should().Be("parasut-123");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task PushIncomeAsync_HttpFailure_ReturnsFalseResult()
    {
        var incomeId = Guid.NewGuid();
        var income = MakeIncome(incomeId);
        _incomeRepoMock.Setup(r => r.GetByIdAsync(incomeId, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("""{"errors":[{"title":"Validation failed"}]}""")
        });

        var service = BuildService(httpClient);
        var result = await service.PushIncomeAsync(incomeId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ── PushExpenseAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task PushExpenseAsync_ExpenseNotFound_ReturnsFalseResult()
    {
        var expenseId = Guid.NewGuid();
        _expenseRepoMock.Setup(r => r.GetByIdAsync(expenseId, It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        var service = BuildService(new HttpClient());
        var result = await service.PushExpenseAsync(expenseId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain(expenseId.ToString());
    }

    [Fact]
    public async Task PushExpenseAsync_ExpenseFound_HttpSuccess_ReturnsTrueWithExternalId()
    {
        var expenseId = Guid.NewGuid();
        var expense = MakeExpense(expenseId);
        _expenseRepoMock.Setup(r => r.GetByIdAsync(expenseId, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var responseJson = """
            {
                "data": {
                    "id": "parasut-456",
                    "type": "purchase_invoices",
                    "attributes": { "status": "draft" }
                }
            }
            """;

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseJson)
        });

        var service = BuildService(httpClient);
        var result = await service.PushExpenseAsync(expenseId);

        result.Success.Should().BeTrue();
        result.ExternalId.Should().Be("parasut-456");
    }

    [Fact]
    public async Task PushExpenseAsync_HttpFailure_ReturnsFalseResult()
    {
        var expenseId = Guid.NewGuid();
        var expense = MakeExpense(expenseId);
        _expenseRepoMock.Setup(r => r.GetByIdAsync(expenseId, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"errors":[{"title":"Bad request"}]}""")
        });

        var service = BuildService(httpClient);
        var result = await service.PushExpenseAsync(expenseId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ── GetBalanceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBalanceAsync_HttpSuccess_ReturnsPopulatedDto()
    {
        var responseJson = """
            {
                "data": [
                    {
                        "id": "acc-1",
                        "type": "accounts",
                        "attributes": {
                            "name": "Ticari Alacaklar",
                            "balance": "5000.00"
                        }
                    },
                    {
                        "id": "acc-2",
                        "type": "accounts",
                        "attributes": {
                            "name": "Kısa Vadeli Borçlar",
                            "balance": "-2000.00"
                        }
                    }
                ]
            }
            """;

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        });

        var service = BuildService(httpClient);
        var result = await service.GetBalanceAsync();

        result.TotalReceivable.Should().Be(5000m);
        result.TotalPayable.Should().Be(2000m);
        result.NetBalance.Should().Be(3000m);
        result.AsOf.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetBalanceAsync_HttpFailure_ReturnsZeroBalance()
    {
        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"errors":[{"title":"Unauthorized"}]}""")
        });

        var service = BuildService(httpClient);
        var result = await service.GetBalanceAsync();

        result.TotalReceivable.Should().Be(0m);
        result.TotalPayable.Should().Be(0m);
        result.NetBalance.Should().Be(0m);
    }

    [Fact]
    public async Task GetBalanceAsync_EmptyDataArray_ReturnsZeroBalance()
    {
        var responseJson = """{ "data": [] }""";

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        });

        var service = BuildService(httpClient);
        var result = await service.GetBalanceAsync();

        result.TotalReceivable.Should().Be(0m);
        result.TotalPayable.Should().Be(0m);
        result.NetBalance.Should().Be(0m);
    }

    // ── GetRecentTransactionsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetRecentTransactionsAsync_HttpSuccess_ReturnsMappedList()
    {
        var responseJson = """
            {
                "data": [
                    {
                        "id": "tx-001",
                        "type": "transaction_documents",
                        "attributes": {
                            "item_type": "income",
                            "net_total": "1500.00",
                            "description": "Trendyol satışı",
                            "issue_date": "2026-03-01"
                        }
                    },
                    {
                        "id": "tx-002",
                        "type": "transaction_documents",
                        "attributes": {
                            "item_type": "expense",
                            "net_total": "300.00",
                            "description": "Kargo gideri",
                            "issue_date": "2026-03-02"
                        }
                    }
                ]
            }
            """;

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        });

        var service = BuildService(httpClient);
        var result = await service.GetRecentTransactionsAsync(days: 30);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("tx-001");
        result[0].Type.Should().Be("income");
        result[0].Amount.Should().Be(1500m);
        result[0].Description.Should().Be("Trendyol satışı");
        result[1].Id.Should().Be("tx-002");
        result[1].Type.Should().Be("expense");
        result[1].Amount.Should().Be(300m);
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_HttpFailure_ReturnsEmptyList()
    {
        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        });

        var service = BuildService(httpClient);
        var result = await service.GetRecentTransactionsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_EmptyDataArray_ReturnsEmptyList()
    {
        var responseJson = """{ "data": [] }""";

        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        });

        var service = BuildService(httpClient);
        var result = await service.GetRecentTransactionsAsync(days: 7);

        result.Should().BeEmpty();
    }

    // ── DTO properties ────────────────────────────────────────────────────

    [Fact]
    public void ParasutSyncResult_DefaultValues_AreCorrect()
    {
        var result = new ParasutSyncResult();
        result.Success.Should().BeFalse();
        result.ExternalId.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ParasutBalanceDto_DefaultValues_AreCorrect()
    {
        var dto = new ParasutBalanceDto();
        dto.TotalReceivable.Should().Be(0m);
        dto.TotalPayable.Should().Be(0m);
        dto.NetBalance.Should().Be(0m);
    }

    [Fact]
    public void ParasutTransactionDto_DefaultStringValues_AreEmpty()
    {
        var dto = new ParasutTransactionDto();
        dto.Id.Should().BeEmpty();
        dto.Type.Should().BeEmpty();
        dto.Description.Should().BeEmpty();
        dto.Amount.Should().Be(0m);
    }
}
