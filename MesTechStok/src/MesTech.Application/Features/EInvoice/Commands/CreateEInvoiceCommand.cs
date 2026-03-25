using FluentValidation;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.EInvoice.Commands;

public record CreateEInvoiceCommand(
    Guid? OrderId,
    string BuyerVkn,
    string BuyerTitle,
    string? BuyerEmail,
    EInvoiceScenario Scenario,
    EInvoiceType Type,
    DateTime IssueDate,
    string CurrencyCode,
    IReadOnlyList<CreateEInvoiceLineRequest> Lines,
    string ProviderId
) : IRequest<Guid>;

public record CreateEInvoiceLineRequest(
    string Description,
    decimal Quantity,
    string UnitCode,
    decimal UnitPrice,
    int TaxPercent,
    decimal AllowanceAmount,
    Guid? ProductId);

public sealed class CreateEInvoiceCommandValidator : AbstractValidator<CreateEInvoiceCommand>
{
    private static readonly int[] ValidTaxRates = { 0, 1, 8, 10, 18, 20 };

    public CreateEInvoiceCommandValidator()
    {
        // K1b-03: e-Arsiv faturada VKN zorunlu degil (bireysel alici 11111111111 kullanilabilir)
        RuleFor(x => x.BuyerVkn)
            .NotEmpty()
            .Length(10, 11)
            .Matches("^[0-9]+$")
            .WithMessage("VKN/TCKN 10 veya 11 haneli rakam olmali.");
        RuleFor(x => x.BuyerTitle).NotEmpty().MaximumLength(512);

        // K1b-03: e-Arsiv faturada alici e-posta zorunlu (GIB kurali)
        RuleFor(x => x.BuyerEmail)
            .NotEmpty()
            .EmailAddress()
            .When(x => x.Scenario == EInvoiceScenario.EARSIVFATURA)
            .WithMessage("e-Arsiv faturada alici e-posta adresi zorunludur.");

        RuleFor(x => x.Lines).NotEmpty()
            .WithMessage("En az 1 fatura satiri gerekli.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(li => li.Quantity).GreaterThan(0);
            l.RuleFor(li => li.UnitPrice).GreaterThan(0);
            l.RuleFor(li => li.TaxPercent)
                .Must(p => ValidTaxRates.Contains(p))
                .WithMessage("Gecersiz KDV orani.");
        });
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.IssueDate)
            .Must(d => d.Date <= DateTime.UtcNow.Date.AddDays(7))
            .WithMessage("Fatura tarihi gelecekte 7 gunden fazla olamaz.");
    }
}
