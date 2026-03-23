using FluentValidation;

namespace MesTech.Application.Commands.SyncBitrix24Contacts;

public class SyncBitrix24ContactsValidator : AbstractValidator<SyncBitrix24ContactsCommand>
{
    public SyncBitrix24ContactsValidator()
    {
        RuleFor(x => x).Custom((_, context) =>
        {
            // Guard: Bitrix24 sync parametresiz tetiklenir.
            // Rate-limit ve bağlantı kontrolü handler seviyesinde yapılır.
        });
    }
}
