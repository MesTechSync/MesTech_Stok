using FluentValidation;

namespace MesTech.Application.Commands.GenerateEFatura;

public sealed class GenerateEFaturaValidator : AbstractValidator<GenerateEFaturaCommand>
{
    public GenerateEFaturaValidator()
    {
        RuleFor(x => x.BotUserId)
            .NotEmpty().WithMessage("Bot kullanıcı ID zorunludur.")
            .MaximumLength(100).WithMessage("Bot kullanıcı ID en fazla 100 karakter.");

        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId boş olamaz.");

        RuleFor(x => x.OrderId)
            .NotEqual(Guid.Empty).When(x => x.OrderId.HasValue)
            .WithMessage("OrderId geçersiz.");

        RuleFor(x => x.BuyerVkn)
            .MaximumLength(11).When(x => !string.IsNullOrWhiteSpace(x.BuyerVkn))
            .WithMessage("VKN en fazla 11 karakter olabilir.")
            .Matches(@"^\d{10,11}$").When(x => !string.IsNullOrWhiteSpace(x.BuyerVkn))
            .WithMessage("VKN 10 veya 11 haneli rakam olmalı.");
    }
}
