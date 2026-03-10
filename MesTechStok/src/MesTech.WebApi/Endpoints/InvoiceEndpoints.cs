using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

internal record InvoiceEndpointRequest(
    int Provider,
    InvoiceCreateRequest Invoice);

public static class InvoiceEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/invoices").WithTags("Invoices");

        // POST /api/v1/invoices — create an invoice via the resolved provider adapter
        group.MapPost("/", async (
            InvoiceEndpointRequest request,
            IInvoiceAdapterFactory factory,
            CancellationToken ct) =>
        {
            try
            {
                var providerType = (InvoiceProvider)request.Provider;
                var adapter = factory.Resolve(providerType);

                if (adapter is null)
                    return Results.BadRequest("Unknown provider");

                var result = await adapter.CreateInvoiceAsync(request.Invoice, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}
