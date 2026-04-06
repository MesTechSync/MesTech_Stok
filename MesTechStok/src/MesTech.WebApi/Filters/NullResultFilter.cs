namespace MesTech.WebApi.Filters;

/// <summary>
/// GET endpoint'lerinde null result döndüğünde otomatik 404 NotFound döner.
/// Handler null dönerse client'a 200+null yerine 404 gider.
///
/// G11015 FIX: 316 Results.Ok(result) pattern'ının sistemik çözümü.
/// Single-entity GET'lerde handler null dönebilir (entity bulunamadı).
/// Liste dönen handler'lar boş array döner (null değil) — bu filter onları etkilemez.
///
/// Kullanım: group.AddEndpointFilter&lt;NullResultFilter&gt;();
/// veya Program.cs'te global: app.MapGroup("/api").AddEndpointFilter&lt;NullResultFilter&gt;();
/// </summary>
public sealed class NullResultFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context).ConfigureAwait(false);

        // Sadece GET isteklerinde kontrol et — POST/PUT/DELETE farklı semantik
        if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
            return result;

        // IResult dönüyorsa (Results.Ok, Results.NotFound vb.) dokunma — zaten handle edilmiş
        if (result is IResult)
            return result;

        // Handler doğrudan null döndüyse → 404
        if (result is null)
            return Results.NotFound();

        return result;
    }
}
