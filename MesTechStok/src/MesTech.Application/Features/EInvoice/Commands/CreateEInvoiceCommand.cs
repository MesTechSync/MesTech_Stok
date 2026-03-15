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

public class CreateEInvoiceCommandValidator : AbstractValidator<CreateEInvoiceCommand>
{
    public CreateEInvoiceCommandValidator()
    {
        RuleFor(x => x.BuyerVkn)
            .NotEmpty()
            .Length(10, 11)
            .Matches("^[0-9]+$")
            .WithMessage("VKN/TCKN 10 veya 11 haneli rakam olmali.");
        RuleFor(x => x.BuyerTitle).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Lines).NotEmpty()
            .WithMessage("En az 1 fatura satiri gerekli.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(li => li.Quantity).GreaterThan(0);
            l.RuleFor(li => li.UnitPrice).GreaterThan(0);
            l.RuleFor(li => li.TaxPercent)
                .Must(p => new[] { 0, 1, 8, 10, 18, 20 }.Contains(p))
                .WithMessage("Gecersiz KDV orani.");
        });
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.IssueDate)
            .Must(d => d.Date <= DateTime.UtcNow.Date.AddDays(7))
            .WithMessage("Fatura tarihi gelecekte 7 gunden fazla olamaz.");
    }
}
