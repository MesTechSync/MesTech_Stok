using FluentValidation;
using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record ExportPoolProductsToPlatformCommand(
    Guid PoolId,
    IEnumerable<Guid> ProductIds,
    string PlatformCode,
    decimal PriceMarkupPercent,
    bool HideSupplierInfo
) : IRequest<ExportToPlatformResult>;

public record ExportToPlatformResult(int Sent, int Failed, IList<string> Errors);

public class ExportPoolProductsToPlatformCommandValidator
    : AbstractValidator<ExportPoolProductsToPlatformCommand>
{
    public ExportPoolProductsToPlatformCommandValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.PlatformCode).NotEmpty();
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0).LessThanOrEqualTo(500);
    }
}

public class ExportPoolProductsToPlatformCommandHandler(
    IDropshippingPoolRepository poolRepo,
    IAdapterFactory adapterFactory
) : IRequestHandler<ExportPoolProductsToPlatformCommand, ExportToPlatformResult>
{
    public async Task<ExportToPlatformResult> Handle(
        ExportPoolProductsToPlatformCommand req, CancellationToken cancellationToken)
    {
        var adapter = adapterFactory.Resolve(req.PlatformCode)
            ?? throw new InvalidOperationException($"Platform bulunamadı: {req.PlatformCode}");

        var products = await poolRepo.GetPoolProductsByIdsAsync(
            req.PoolId, req.ProductIds, cancellationToken);

        int sent = 0, failed = 0;
        var errors = new List<string>();

        foreach (var p in products)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var ok = await adapter.PushProductAsync(p.Product!, cancellationToken);
                if (ok) sent++;
                else { failed++; errors.Add($"SKU:{p.Product?.SKU} gönderilemedi"); }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"SKU:{p.Product?.SKU} — {ex.Message}");
            }
        }

        return new ExportToPlatformResult(sent, failed, errors);
    }
}
