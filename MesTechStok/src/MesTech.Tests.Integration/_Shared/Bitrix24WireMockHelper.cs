using System.Text.Json;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// WireMock response builder for Bitrix24 REST API contract tests.
/// OAuth2 token, CRM deal/contact/product, batch, and error responses.
/// </summary>
public static class Bitrix24WireMockHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // ══════════════════════════════════════
    // OAuth 2.0 Token Responses
    // ══════════════════════════════════════

    public static string BuildTokenResponse(
        string accessToken = "test-b24-access-token",
        string refreshToken = "test-b24-refresh-token-new",
        int expiresIn = 1800,
        string memberId = "abc123def456",
        string domain = "mestech-test.bitrix24.com")
    {
        return JsonSerializer.Serialize(new
        {
            access_token = accessToken,
            refresh_token = refreshToken,
            expires_in = expiresIn,
            token_type = "Bearer",
            scope = "crm",
            member_id = memberId,
            domain
        }, JsonOptions);
    }

    public static string BuildTokenErrorResponse(
        string error = "invalid_grant",
        string description = "The refresh token is invalid or expired.")
    {
        return JsonSerializer.Serialize(new
        {
            error,
            error_description = description
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // CRM Deal Responses
    // ══════════════════════════════════════

    public static string BuildDealListResponse(
        (int id, string title, decimal opportunity, string stage)[]? deals = null)
    {
        var dealList = deals ?? new[]
        {
            (1, "Deal 1", 1500.00m, "NEW"),
            (2, "Deal 2", 2500.00m, "WON")
        };

        var result = dealList.Select(d => new
        {
            ID = d.id,
            TITLE = d.title,
            OPPORTUNITY = d.opportunity.ToString("F2"),
            STAGE_ID = d.stage,
            CURRENCY_ID = "TRY"
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            result,
            total = result.Length
        }, JsonOptions);
    }

    public static string BuildDealAddResponse(int dealId = 100)
    {
        return JsonSerializer.Serialize(new { result = dealId }, JsonOptions);
    }

    public static string BuildDealUpdateResponse(bool success = true)
    {
        return JsonSerializer.Serialize(new { result = success }, JsonOptions);
    }

    // ══════════════════════════════════════
    // CRM Contact Responses
    // ══════════════════════════════════════

    public static string BuildContactListResponse(
        (int id, string name, string lastName, string? phone, string? email)[]? contacts = null)
    {
        var contactList = contacts ?? new[]
        {
            (1, "Ali", "Yilmaz", (string?)"+905551234567", (string?)"ali@test.com"),
            (2, "Ayse", "Kaya", (string?)"+905559876543", (string?)"ayse@test.com")
        };

        var result = contactList.Select(c => new
        {
            ID = c.id,
            NAME = c.name,
            LAST_NAME = c.lastName,
            PHONE = c.phone != null ? new[] { new { VALUE = c.phone, VALUE_TYPE = "WORK" } } : Array.Empty<object>(),
            EMAIL = c.email != null ? new[] { new { VALUE = c.email, VALUE_TYPE = "WORK" } } : Array.Empty<object>()
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            result,
            total = result.Length
        }, JsonOptions);
    }

    public static string BuildContactAddResponse(int contactId = 200)
    {
        return JsonSerializer.Serialize(new { result = contactId }, JsonOptions);
    }

    // ══════════════════════════════════════
    // CRM Product Responses
    // ══════════════════════════════════════

    public static string BuildProductListResponse(
        (int id, string name, decimal price)[]? products = null)
    {
        var productList = products ?? new[]
        {
            (1, "Product A", 99.90m),
            (2, "Product B", 149.90m)
        };

        var result = productList.Select(p => new
        {
            ID = p.id,
            NAME = p.name,
            PRICE = p.price.ToString("F2"),
            CURRENCY_ID = "TRY",
            DESCRIPTION = $"Description for {p.name}"
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            result,
            total = result.Length
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Catalog Section Responses
    // ══════════════════════════════════════

    public static string BuildCatalogSectionListResponse(
        (int id, string name, int? sectionId)[]? sections = null)
    {
        var sectionList = sections ?? new (int, string, int?)[]
        {
            (1, "Electronics", null),
            (2, "Phones", 1),
            (3, "Accessories", 1)
        };

        var result = new
        {
            sections = sectionList.Select(s => new
            {
                id = s.Item1,
                name = s.Item2,
                sectionId = s.Item3
            }).ToArray()
        };

        return JsonSerializer.Serialize(new { result }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Batch Responses
    // ══════════════════════════════════════

    public static string BuildBatchResponse(
        Dictionary<string, object>? results = null,
        Dictionary<string, object>? errors = null)
    {
        var batchResults = results ?? new Dictionary<string, object>
        {
            ["cmd0"] = true,
            ["cmd1"] = true
        };

        var batchErrors = errors ?? new Dictionary<string, object>();

        return JsonSerializer.Serialize(new
        {
            result = new
            {
                result = batchResults,
                result_error = batchErrors,
                result_total = new Dictionary<string, int>()
            }
        }, JsonOptions);
    }

    public static string BuildBatchPartialFailureResponse()
    {
        return JsonSerializer.Serialize(new
        {
            result = new
            {
                result = new Dictionary<string, object>
                {
                    ["cmd0"] = 100,
                    ["cmd2"] = 200
                },
                result_error = new Dictionary<string, object>
                {
                    ["cmd1"] = new { error = "ACCESS_DENIED", error_description = "Access denied" }
                },
                result_total = new Dictionary<string, int>()
            }
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Profile Response (TestConnection)
    // ══════════════════════════════════════

    public static string BuildProfileResponse(
        string name = "MesTech Admin",
        string lastName = "Test",
        int id = 1)
    {
        return JsonSerializer.Serialize(new
        {
            result = new
            {
                ID = id,
                NAME = name,
                LAST_NAME = lastName,
                ADMIN = true
            }
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Error Responses
    // ══════════════════════════════════════

    public static string BuildErrorResponse(
        string error = "QUERY_LIMIT_EXCEEDED",
        string description = "Too many requests")
    {
        return JsonSerializer.Serialize(new
        {
            error,
            error_description = description
        }, JsonOptions);
    }

    public static string BuildAccessDeniedResponse()
    {
        return BuildErrorResponse("NO_AUTH_FOUND", "Authorization required");
    }
}
