using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;
using MesTech.Application.Features.Stores.Commands.SaveStoreCredential;
using MesTech.Application.Features.Stores.Commands.TestStoreCredential;
using MesTech.Application.Features.Stores.Queries.GetStoreCredential;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Store credential CRUD + test endpoints.
/// Credential degerleri DB'de AES-256-GCM ile sifrelenir,
/// GET endpoint'i sadece maskelenmis degerleri dondurur.
/// </summary>
public static class StoreCredentialEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/stores/{storeId:guid}/credentials")
            .WithTags("Store Credentials")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/stores/{storeId}/credentials — credential kaydet (upsert)
        group.MapPost("/", async (
            Guid storeId,
            SaveStoreCredentialCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            // Override storeId from route
            var cmd = command with { StoreId = storeId };
            var id = await mediator.Send(cmd, ct);
            return Results.Created(
                $"/api/v1/stores/{storeId}/credentials",
                new { id, storeId });
        })
        .WithName("SaveStoreCredential")
        .WithSummary("Magaza credential'larini kaydet/guncelle (upsert, AES-256-GCM sifreleme)");

        // POST /api/v1/stores/{storeId}/credentials/test — baglanti testi
        group.MapPost("/test", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new TestStoreCredentialCommand(storeId), ct);
            return result.Success
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        })
        .WithName("TestStoreCredential")
        .WithSummary("Kaydedilmis credential'lar ile platform baglanti testi");

        // GET /api/v1/stores/{storeId}/credentials — maskelenmis credential bilgisi
        group.MapGet("/", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetStoreCredentialQuery(storeId), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { Message = $"No credentials found for store {storeId}" });
        })
        .CacheOutput("Lookup60s")
        .WithName("GetStoreCredentials")
        .WithSummary("Maskelenmis credential bilgisi (plaintext DONMEZ)");

        // DELETE /api/v1/stores/{storeId}/credentials — soft-delete
        group.MapDelete("/", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(
                new DeleteStoreCredentialCommand(storeId), ct);
            return success
                ? Results.NoContent()
                : Results.NotFound(new { Message = $"No credentials found for store {storeId}" });
        })
        .WithName("DeleteStoreCredential")
        .WithSummary("Magaza credential'larini soft-delete et");
    }
}
