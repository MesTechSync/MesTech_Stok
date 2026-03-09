using System.Text.Json;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// JSON:API response builder for Parasut WireMock stubs.
/// Spec: https://jsonapi.org/format/
/// Content-Type: application/vnd.api+json
/// </summary>
public static class JsonApiHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    /// <summary>
    /// Builds a single resource response: { data: { type, id, attributes } }
    /// </summary>
    public static string BuildResource(string type, string id, Dictionary<string, object> attributes)
    {
        var resource = new
        {
            data = new
            {
                type,
                id,
                attributes
            }
        };
        return JsonSerializer.Serialize(resource, JsonOpts);
    }

    /// <summary>
    /// Builds a collection response with pagination meta:
    /// { data: [...], meta: { total_count, current_page, per_page, total_pages } }
    /// </summary>
    public static string BuildCollection(
        string type,
        List<(string id, Dictionary<string, object> attrs)> items,
        int totalCount,
        int currentPage,
        int pageSize)
    {
        var data = items.Select(i => new
        {
            type,
            id = i.id,
            attributes = i.attrs
        }).ToArray();

        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        var response = new
        {
            data,
            meta = new
            {
                total_count = totalCount,
                current_page = currentPage,
                per_page = pageSize,
                total_pages = totalPages
            }
        };
        return JsonSerializer.Serialize(response, JsonOpts);
    }

    /// <summary>
    /// Builds a JSON:API error response: { errors: [{ status, title, detail }] }
    /// </summary>
    public static string BuildError(int status, string title, string detail)
    {
        var error = new
        {
            errors = new[]
            {
                new
                {
                    status = status.ToString(),
                    title,
                    detail
                }
            }
        };
        return JsonSerializer.Serialize(error, JsonOpts);
    }

    /// <summary>
    /// Builds an empty collection: { data: [] }
    /// </summary>
    public static string BuildEmptyCollection()
    {
        return @"{""data"":[]}";
    }
}
