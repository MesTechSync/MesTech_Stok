using System.Security.Claims;
using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Filters;

/// <summary>
/// OWASP ASVS V4 — endpoint seviyesinde permission kontrolu.
/// Kullanim: endpoints.RequirePermission("ManageProducts")
/// </summary>
public sealed class RequirePermissionFilter : IEndpointFilter
{
    private readonly string _permissionName;

    public RequirePermissionFilter(string permissionName)
    {
        _permissionName = permissionName;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        bool hasPermission;
        try
        {
            hasPermission = await permissionService.HasPermissionAsync(userId, _permissionName).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionFilter>>();
            logger.LogError(ex, "Permission check failed for user {UserId}, permission {Permission}", userId, _permissionName);
            return Results.Problem(detail: "Yetki kontrolu sirasinda hata olustu. Lutfen tekrar deneyin.", statusCode: 503);
        }

        if (!hasPermission)
            return Results.Forbid();

        return await next(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Extension method: group.MapGet("/products", handler).RequirePermission("ManageProducts")
/// </summary>
public static class RequirePermissionExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permissionName)
    {
        return builder.AddEndpointFilter(new RequirePermissionFilter(permissionName));
    }
}
